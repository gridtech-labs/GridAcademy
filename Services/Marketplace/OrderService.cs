using GridAcademy.Data;
using GridAcademy.Data.Entities.Marketplace;
using GridAcademy.DTOs.Marketplace;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GridAcademy.Services.Marketplace;

public class OrderService : IOrderService
{
    private readonly AppDbContext        _db;
    private readonly IRazorpayService    _razorpay;
    private readonly IConfiguration      _config;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        AppDbContext db,
        IRazorpayService razorpay,
        IConfiguration config,
        ILogger<OrderService> logger)
    {
        _db       = db;
        _razorpay = razorpay;
        _config   = config;
        _logger   = logger;
    }

    // ── Create Order ─────────────────────────────────────────────────────────
    public async Task<CreateOrderResponse> CreateAsync(Guid studentId, CreateOrderRequest req, CancellationToken ct = default)
    {
        var series = await _db.MpTestSeries
            .FirstOrDefaultAsync(s => s.Id == req.SeriesId && s.Status == Data.Entities.Marketplace.SeriesStatus.Published && s.IsActive, ct)
            ?? throw new KeyNotFoundException("Test series not found or not available for purchase.");

        // Check no existing PAID entitlement
        var hasAccess = await _db.MpEntitlements
            .AnyAsync(e => e.StudentId == studentId && e.SeriesId == req.SeriesId
                        && (e.ExpiresAt == null || e.ExpiresAt > DateTime.UtcNow), ct);
        if (hasAccess)
            throw new InvalidOperationException("You already have access to this test series.");

        // Tax and fee config from appsettings (defaults: 18% GST, 2% booking fee)
        var gstPct     = decimal.Parse(_config["Marketplace:GstPct"]         ?? "18");
        var bookingPct = decimal.Parse(_config["Marketplace:BookingFeePct"]  ?? "2");

        var baseAmount = series.PriceInr;

        // Promo code discount
        decimal discount = 0;
        string? promoApplied = null;

        if (!string.IsNullOrWhiteSpace(req.PromoCode))
        {
            var promoReq = new PromoCodeValidateRequest(req.PromoCode, req.SeriesId, baseAmount);
            var promoResult = await ValidatePromoInternalAsync(promoReq, ct);
            if (promoResult.IsValid)
            {
                discount     = promoResult.DiscountAmount;
                promoApplied = req.PromoCode.Trim().ToUpperInvariant();
                baseAmount   = promoResult.FinalAmount;
            }
        }

        var gstAmount    = Math.Round(baseAmount * gstPct / 100, 2);
        var bookingFee   = Math.Round(baseAmount * bookingPct / 100, 2);
        var grandTotal   = baseAmount + gstAmount + bookingFee;

        // BookingRef: GA-YYYYMMDD-XXXX (random 4-char alphanumeric)
        var bookingRef = $"GA-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..4].ToUpperInvariant()}";

        // Create order in DB (Pending status)
        var order = new MpOrder
        {
            StudentId      = studentId,
            SeriesId       = req.SeriesId,
            AmountInr      = series.PriceInr,
            GstAmount      = gstAmount,
            BookingFee     = bookingFee,
            DiscountApplied = discount,
            GrandTotal     = grandTotal,
            PromoCodeApplied = promoApplied,
            Status         = OrderStatus.Pending,
            BookingRef     = bookingRef,
            CreatedAt      = DateTime.UtcNow,
            UpdatedAt      = DateTime.UtcNow
        };

        _db.MpOrders.Add(order);
        await _db.SaveChangesAsync(ct);

        // Create Razorpay order (only if amount > 0)
        string razorpayOrderId;
        if (grandTotal > 0)
        {
            razorpayOrderId = await _razorpay.CreateOrderAsync(grandTotal, bookingRef, ct);
            order.RazorpayOrderId = razorpayOrderId;
            await _db.SaveChangesAsync(ct);
        }
        else
        {
            // Free series: auto-grant entitlement
            razorpayOrderId = string.Empty;
            await GrantEntitlementAsync(order, ct);
        }

        var keyId = _config["Razorpay:KeyId"] ?? "";

        return new CreateOrderResponse(
            order.Id,
            bookingRef,
            razorpayOrderId,
            keyId,
            series.PriceInr,
            gstAmount,
            bookingFee,
            discount,
            grandTotal);
    }

    // ── Verify Payment ────────────────────────────────────────────────────────
    public async Task<bool> VerifyPaymentAsync(Guid studentId, VerifyPaymentRequest req, CancellationToken ct = default)
    {
        var order = await _db.MpOrders
            .FirstOrDefaultAsync(o => o.Id == req.OrderId && o.StudentId == studentId, ct)
            ?? throw new KeyNotFoundException("Order not found.");

        if (order.Status == OrderStatus.Paid)
            return true; // idempotent

        if (order.Status != OrderStatus.Pending)
            throw new InvalidOperationException("Order is not in Pending state.");

        // Verify Razorpay signature
        if (!_razorpay.VerifySignature(req.RazorpayOrderId, req.RazorpayPaymentId, req.RazorpaySignature))
        {
            order.Status    = OrderStatus.Failed;
            order.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            _logger.LogWarning("Payment signature verification failed for order {OrderId}", order.Id);
            return false;
        }

        // Mark order paid
        order.Status             = OrderStatus.Paid;
        order.RazorpayPaymentId  = req.RazorpayPaymentId;
        order.UpdatedAt          = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        // Grant entitlement + record commission
        await GrantEntitlementAsync(order, ct);
        await RecordCommissionAsync(order, ct);

        // Increment purchase count + promo used count
        var series = await _db.MpTestSeries.FindAsync(new object[] { order.SeriesId }, ct);
        if (series is not null)
        {
            series.PurchaseCount++;
            series.UpdatedAt = DateTime.UtcNow;
        }

        if (!string.IsNullOrWhiteSpace(order.PromoCodeApplied))
        {
            var promo = await _db.MpPromoCodes
                .FirstOrDefaultAsync(p => p.Code == order.PromoCodeApplied, ct);
            if (promo is not null) promo.UsedCount++;
        }

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Payment verified and entitlement granted for order {OrderId}", order.Id);
        return true;
    }

    // ── Check Entitlement ─────────────────────────────────────────────────────
    public async Task<EntitlementCheckResponse> CheckEntitlementAsync(Guid studentId, Guid seriesId, CancellationToken ct = default)
    {
        var ent = await _db.MpEntitlements
            .FirstOrDefaultAsync(e => e.StudentId == studentId && e.SeriesId == seriesId, ct);

        if (ent is null)
            return new EntitlementCheckResponse(false, null, null, false);

        var isExpired = ent.ExpiresAt.HasValue && ent.ExpiresAt < DateTime.UtcNow;
        return new EntitlementCheckResponse(!isExpired, ent.Id, ent.ExpiresAt, isExpired);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task GrantEntitlementAsync(MpOrder order, CancellationToken ct)
    {
        // Avoid duplicate entitlements (idempotent)
        var exists = await _db.MpEntitlements.AnyAsync(
            e => e.StudentId == order.StudentId && e.SeriesId == order.SeriesId, ct);
        if (exists) return;

        _db.MpEntitlements.Add(new MpEntitlement
        {
            StudentId  = order.StudentId,
            SeriesId   = order.SeriesId,
            OrderId    = order.Id,
            ExpiresAt  = null,   // lifetime access (can be changed per series later)
            GrantedAt  = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);
    }

    private async Task RecordCommissionAsync(MpOrder order, CancellationToken ct)
    {
        var exists = await _db.MpCommissions.AnyAsync(c => c.OrderId == order.Id, ct);
        if (exists) return;

        var platformPct  = decimal.Parse(_config["Marketplace:PlatformCommissionPct"] ?? "30");
        var providerPct  = 100 - platformPct;
        var gross        = order.AmountInr; // commission calculated on base price, excl. tax/fee

        var series = await _db.MpTestSeries.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == order.SeriesId, ct);
        if (series is null) return;

        _db.MpCommissions.Add(new MpCommission
        {
            OrderId        = order.Id,
            ProviderId     = series.ProviderId,
            GrossAmount    = gross,
            PlatformPct    = platformPct,
            PlatformAmount = Math.Round(gross * platformPct / 100, 2),
            ProviderPct    = providerPct,
            ProviderAmount = Math.Round(gross * providerPct / 100, 2),
            Status         = CommissionStatus.Pending,
            CreatedAt      = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);
    }

    private async Task<PromoCodeValidateResponse> ValidatePromoInternalAsync(PromoCodeValidateRequest req, CancellationToken ct)
    {
        var now  = DateTime.UtcNow;
        var code = req.Code.Trim().ToUpperInvariant();

        var promo = await _db.MpPromoCodes.FirstOrDefaultAsync(p =>
            p.Code == code && p.IsActive &&
            (p.ValidFrom == null || p.ValidFrom <= now) &&
            (p.ValidTo   == null || p.ValidTo   >= now), ct);

        if (promo is null)
            return new PromoCodeValidateResponse(false, "Invalid promo code.", 0, "Flat", req.OrderAmount);

        if (promo.UsageLimit.HasValue && promo.UsedCount >= promo.UsageLimit.Value)
            return new PromoCodeValidateResponse(false, "Promo usage limit reached.", 0, "Flat", req.OrderAmount);

        decimal discount = promo.DiscountType == DiscountType.Percentage
            ? req.OrderAmount * promo.DiscountValue / 100
            : promo.DiscountValue;

        if (promo.MaxDiscount.HasValue && discount > promo.MaxDiscount.Value)
            discount = promo.MaxDiscount.Value;

        discount = Math.Min(discount, req.OrderAmount);

        return new PromoCodeValidateResponse(true, null, discount, promo.DiscountType.ToString(), req.OrderAmount - discount);
    }
}
