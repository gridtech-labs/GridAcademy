using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GridAcademy.Data;
using GridAcademy.Data.Entities.Payment;
using GridAcademy.DTOs.Payment;
using GridAcademy.Services.Marketplace;
using Microsoft.EntityFrameworkCore;

namespace GridAcademy.Services.Payment;

public class ExamPaymentService : IExamPaymentService
{
    private readonly AppDbContext          _db;
    private readonly IRazorpayService      _razorpay;
    private readonly IConfiguration        _config;
    private readonly ILogger<ExamPaymentService> _logger;

    public ExamPaymentService(
        AppDbContext db,
        IRazorpayService razorpay,
        IConfiguration config,
        ILogger<ExamPaymentService> logger)
    {
        _db       = db;
        _razorpay = razorpay;
        _config   = config;
        _logger   = logger;
    }

    // ── Create Order ──────────────────────────────────────────────────────────
    public async Task<CreateExamOrderResponse> CreateOrderAsync(Guid studentId, CreateExamOrderRequest req, CancellationToken ct = default)
    {
        var exam = await _db.ExamPages
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == req.ExamPageId, ct)
            ?? throw new KeyNotFoundException("Exam not found.");

        if (exam.PriceInr <= 0)
            throw new InvalidOperationException("This exam is free. No payment required.");

        // Block if already has valid access
        var existingAccess = await _db.ExamAccesses
            .AnyAsync(a => a.StudentId == studentId && a.ExamPageId == req.ExamPageId
                       && a.IsActive && (a.ExpiresAt == null || a.ExpiresAt > DateTime.UtcNow), ct);
        if (existingAccess)
            throw new InvalidOperationException("You already have access to this exam.");

        var student = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == studentId, ct)
            ?? throw new KeyNotFoundException("Student not found.");

        decimal originalAmount = exam.PriceInr;
        decimal discountAmount = 0;
        decimal finalAmount    = originalAmount;
        int?    offerId        = null;
        string? offerTitle     = null;
        string? offerCode      = null;

        // Apply offer if provided
        if (!string.IsNullOrWhiteSpace(req.OfferCode))
        {
            var offerSvc = new ExamOfferService(_db, _logger as ILogger<ExamOfferService> ?? throw new InvalidOperationException());
            var offerResult = await offerSvc.ValidateAsync(new ValidateOfferRequest(req.OfferCode, req.ExamPageId, originalAmount), ct);
            if (offerResult.IsValid)
            {
                discountAmount = offerResult.DiscountAmount;
                finalAmount    = offerResult.FinalAmount;
                offerId        = offerResult.OfferId;
                offerTitle     = offerResult.OfferTitle;
                offerCode      = req.OfferCode.Trim().ToUpperInvariant();
            }
        }

        var gstPct    = decimal.Parse(_config["Marketplace:GstPct"] ?? "18");
        var gstAmount = Math.Round(finalAmount * gstPct / 100, 2);
        var grandTotal = finalAmount + gstAmount;

        var bookingRef = $"GA-EXAM-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";

        // Create order in DB
        var order = new ExamOrder
        {
            StudentId      = studentId,
            ExamPageId     = req.ExamPageId,
            OriginalAmount = originalAmount,
            DiscountAmount = discountAmount,
            FinalAmount    = finalAmount,
            GstAmount      = gstAmount,
            GrandTotal     = grandTotal,
            OfferId        = offerId,
            OfferCode      = offerCode,
            Status         = ExamOrderStatus.Initiated,
            BookingRef     = bookingRef
        };
        _db.ExamOrders.Add(order);
        await _db.SaveChangesAsync(ct);

        // Log transaction
        await LogTransactionAsync(order.Id, "initiated", null, null, grandTotal, ct);

        // If FreeAccess offer → grant directly, skip Razorpay
        if (finalAmount <= 0)
        {
            await GrantAccessAsync(order, ct);
            return BuildResponse(order, "", exam, student, offerTitle);
        }

        // Create Razorpay order
        var razorpayOrderId = await _razorpay.CreateOrderAsync(grandTotal, bookingRef, ct);
        order.RazorpayOrderId = razorpayOrderId;
        order.Status          = ExamOrderStatus.Pending;
        order.UpdatedAt       = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        await LogTransactionAsync(order.Id, "razorpay.order.created", null, razorpayOrderId, grandTotal, ct);

        var keyId = GetActiveKeyId();
        return BuildResponse(order, keyId, exam, student, offerTitle);
    }

    // ── Verify Payment ────────────────────────────────────────────────────────
    public async Task<bool> VerifyPaymentAsync(Guid studentId, VerifyExamPaymentRequest req, CancellationToken ct = default)
    {
        var order = await _db.ExamOrders
            .Include(o => o.ExamPage)
            .FirstOrDefaultAsync(o => o.Id == req.OrderId && o.StudentId == studentId, ct);

        if (order == null)
        {
            _logger.LogWarning("VerifyPayment: order {Id} not found for student {Student}", req.OrderId, studentId);
            return false;
        }

        if (order.Status == ExamOrderStatus.Captured)
        {
            _logger.LogInformation("VerifyPayment: order {Id} already captured", req.OrderId);
            return true; // idempotent
        }

        // Verify signature
        var valid = _razorpay.VerifySignature(req.RazorpayOrderId, req.RazorpayPaymentId, req.RazorpaySignature);

        if (!valid)
        {
            order.Status        = ExamOrderStatus.Failed;
            order.FailureReason = "Razorpay signature verification failed.";
            order.UpdatedAt     = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            await LogTransactionAsync(order.Id, "payment.failed", req.RazorpayPaymentId, req.RazorpayOrderId, order.GrandTotal, ct);
            return false;
        }

        // Mark captured
        order.RazorpayPaymentId = req.RazorpayPaymentId;
        order.RazorpaySignature = req.RazorpaySignature;
        order.Status            = ExamOrderStatus.Captured;
        order.PaidAt            = DateTime.UtcNow;
        order.UpdatedAt         = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        await LogTransactionAsync(order.Id, "payment.captured", req.RazorpayPaymentId, req.RazorpayOrderId, order.GrandTotal, ct);

        // Grant access
        await GrantAccessAsync(order, ct);

        // Increment offer usage
        if (order.OfferId.HasValue)
        {
            var offer = await _db.ExamOffers.FindAsync([order.OfferId.Value], ct);
            if (offer != null) { offer.UsedCount++; await _db.SaveChangesAsync(ct); }
        }

        return true;
    }

    // ── Check Access ──────────────────────────────────────────────────────────
    public async Task<ExamAccessResponse> CheckAccessAsync(Guid studentId, Guid examPageId, CancellationToken ct = default)
    {
        var access = await _db.ExamAccesses
            .AsNoTracking()
            .Where(a => a.StudentId == studentId && a.ExamPageId == examPageId && a.IsActive)
            .OrderByDescending(a => a.GrantedAt)
            .FirstOrDefaultAsync(ct);

        if (access == null) return new ExamAccessResponse(false, null);
        if (access.ExpiresAt.HasValue && access.ExpiresAt < DateTime.UtcNow) return new ExamAccessResponse(false, access.ExpiresAt);
        return new ExamAccessResponse(true, access.ExpiresAt);
    }

    // ── Order List (admin) ────────────────────────────────────────────────────
    public async Task<List<ExamOrderListItem>> GetOrdersAsync(ExamOrderStatus? status, int page, int pageSize, CancellationToken ct = default)
    {
        var q = _db.ExamOrders
            .Include(o => o.Student)
            .Include(o => o.ExamPage)
            .AsNoTracking();

        if (status.HasValue) q = q.Where(o => o.Status == status.Value);

        return await q
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new ExamOrderListItem(
                o.Id, o.BookingRef,
                o.Student.FirstName + " " + o.Student.LastName,
                o.Student.Email,
                o.ExamPage.Title,
                o.GrandTotal, o.Status, o.CreatedAt, o.PaidAt, o.RazorpayPaymentId))
            .ToListAsync(ct);
    }

    // ── Order Detail (admin) ──────────────────────────────────────────────────
    public async Task<ExamOrderDetail?> GetOrderDetailAsync(Guid orderId, CancellationToken ct = default)
    {
        var o = await _db.ExamOrders
            .Include(o => o.Student)
            .Include(o => o.ExamPage)
            .Include(o => o.Transactions)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == orderId, ct);

        if (o == null) return null;

        return new ExamOrderDetail(
            o.Id, o.BookingRef,
            o.Student.FirstName + " " + o.Student.LastName,
            o.Student.Email,
            o.ExamPage.Title, o.ExamPageId,
            o.OriginalAmount, o.DiscountAmount, o.GstAmount, o.GrandTotal,
            o.OfferCode, o.Status,
            o.RazorpayOrderId, o.RazorpayPaymentId, o.FailureReason,
            o.CreatedAt, o.PaidAt,
            o.Transactions.OrderBy(t => t.CreatedAt)
                .Select(t => new ExamOrderTransactionDto(t.Id, t.Event, t.RazorpayPaymentId, t.Amount, t.CreatedAt))
                .ToList());
    }

    // ── Webhook ───────────────────────────────────────────────────────────────
    public async Task<bool> ProcessWebhookAsync(string payload, string signature, CancellationToken ct = default)
    {
        var webhookSecret = _config["Razorpay:WebhookSecret"];
        if (!string.IsNullOrEmpty(webhookSecret))
        {
            // Verify webhook signature: HMAC-SHA256 of payload using webhook secret
            var keyBytes = Encoding.UTF8.GetBytes(webhookSecret);
            var msgBytes = Encoding.UTF8.GetBytes(payload);
            using var hmac = new HMACSHA256(keyBytes);
            var computed  = BitConverter.ToString(hmac.ComputeHash(msgBytes)).Replace("-", "").ToLowerInvariant();
            if (computed != signature.ToLowerInvariant())
            {
                _logger.LogWarning("Webhook signature mismatch");
                return false;
            }
        }

        try
        {
            using var doc  = JsonDocument.Parse(payload);
            var eventName  = doc.RootElement.GetProperty("event").GetString() ?? "";
            var paymentId  = doc.RootElement.TryGetProperty("payload", out var pl)
                && pl.TryGetProperty("payment", out var p)
                && p.TryGetProperty("entity", out var pe)
                && pe.TryGetProperty("id", out var pid)
                    ? pid.GetString() : null;
            var orderId    = doc.RootElement.TryGetProperty("payload", out var pl2)
                && pl2.TryGetProperty("payment", out var p2)
                && p2.TryGetProperty("entity", out var pe2)
                && pe2.TryGetProperty("order_id", out var oid)
                    ? oid.GetString() : null;

            _logger.LogInformation("Webhook received: {Event}, payment={PaymentId}, order={OrderId}", eventName, paymentId, orderId);

            if (eventName == "payment.captured" && orderId != null)
            {
                var order = await _db.ExamOrders
                    .FirstOrDefaultAsync(o => o.RazorpayOrderId == orderId, ct);

                if (order != null && order.Status != ExamOrderStatus.Captured)
                {
                    order.RazorpayPaymentId = paymentId;
                    order.Status  = ExamOrderStatus.Captured;
                    order.PaidAt  = DateTime.UtcNow;
                    order.UpdatedAt = DateTime.UtcNow;
                    await _db.SaveChangesAsync(ct);
                    await LogTransactionAsync(order.Id, "webhook.payment.captured", paymentId, orderId, order.GrandTotal, ct);
                    await GrantAccessAsync(order, ct);
                }
            }
            else if (eventName == "payment.failed" && orderId != null)
            {
                var order = await _db.ExamOrders
                    .FirstOrDefaultAsync(o => o.RazorpayOrderId == orderId, ct);

                if (order != null && order.Status == ExamOrderStatus.Pending)
                {
                    var reason = doc.RootElement.TryGetProperty("payload", out var pl3)
                        && pl3.TryGetProperty("payment", out var p3)
                        && p3.TryGetProperty("entity", out var pe3)
                        && pe3.TryGetProperty("error_description", out var ed)
                            ? ed.GetString() : "Payment failed.";
                    order.Status        = ExamOrderStatus.Failed;
                    order.FailureReason = reason;
                    order.UpdatedAt     = DateTime.UtcNow;
                    await _db.SaveChangesAsync(ct);
                    await LogTransactionAsync(order.Id, "webhook.payment.failed", paymentId, orderId, order.GrandTotal, ct);
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Webhook processing error");
            return false;
        }
    }

    // ── Private Helpers ───────────────────────────────────────────────────────
    private async Task GrantAccessAsync(ExamOrder order, CancellationToken ct)
    {
        var already = await _db.ExamAccesses.AnyAsync(
            a => a.StudentId == order.StudentId && a.ExamPageId == order.ExamPageId && a.IsActive, ct);
        if (already) return;

        _db.ExamAccesses.Add(new ExamAccess
        {
            StudentId  = order.StudentId,
            ExamPageId = order.ExamPageId,
            OrderId    = order.Id,
            GrantedAt  = DateTime.UtcNow,
            ExpiresAt  = null, // lifetime by default
            IsActive   = true
        });
        await _db.SaveChangesAsync(ct);
    }

    private async Task LogTransactionAsync(Guid orderId, string @event, string? paymentId, string? razorpayOrderId, decimal? amount, CancellationToken ct)
    {
        _db.ExamOrderTransactions.Add(new ExamOrderTransaction
        {
            ExamOrderId        = orderId,
            Event              = @event,
            RazorpayPaymentId  = paymentId,
            RazorpayOrderId    = razorpayOrderId,
            Amount             = amount,
            CreatedAt          = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);
    }

    private string GetActiveKeyId()
    {
        var isLive = bool.TryParse(_config["Razorpay:IsLive"], out var live) && live;
        return isLive
            ? _config["Razorpay:LiveKeyId"] ?? _config["Razorpay:KeyId"] ?? ""
            : _config["Razorpay:TestKeyId"] ?? _config["Razorpay:KeyId"] ?? "";
    }

    private static CreateExamOrderResponse BuildResponse(ExamOrder o, string keyId,
        GridAcademy.Data.Entities.Exam.ExamPage exam,
        GridAcademy.Data.Entities.User student,
        string? offerTitle)
        => new(o.Id, o.BookingRef, o.RazorpayOrderId ?? "", keyId,
               o.OriginalAmount, o.DiscountAmount, o.GstAmount, o.GrandTotal,
               offerTitle, exam.Title, exam.Slug,
               student.FirstName + " " + student.LastName, student.Email);
}
