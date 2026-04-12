using System.ComponentModel.DataAnnotations;
using GridAcademy.Data.Entities;
using GridAcademy.Data.Entities.Exam;

namespace GridAcademy.Data.Entities.Payment;

/// <summary>Purchase order for an exam page (paid test paper).</summary>
public class ExamOrder
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid StudentId   { get; set; }
    public Guid ExamPageId  { get; set; }

    // Pricing breakdown
    public decimal OriginalAmount  { get; set; }  // base price before discount
    public decimal DiscountAmount  { get; set; }  // discount applied
    public decimal FinalAmount     { get; set; }  // after discount, before tax
    public decimal GstAmount       { get; set; }  // 18% GST
    public decimal GrandTotal      { get; set; }  // amount charged to student

    // Offer
    public int?    OfferId    { get; set; }
    [MaxLength(50)]
    public string? OfferCode  { get; set; }

    // Razorpay
    [MaxLength(100)] public string? RazorpayOrderId   { get; set; }
    [MaxLength(100)] public string? RazorpayPaymentId { get; set; }
    [MaxLength(256)] public string? RazorpaySignature { get; set; }

    public ExamOrderStatus Status { get; set; } = ExamOrderStatus.Initiated;

    [Required, MaxLength(30)]
    public string BookingRef { get; set; } = string.Empty;  // GA-EXAM-YYYYMMDD-XXXX

    [MaxLength(500)] public string? FailureReason { get; set; }
    [MaxLength(500)] public string? Notes         { get; set; }

    public DateTime  CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime  UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAt    { get; set; }

    // Navigation
    public User       Student   { get; set; } = null!;
    public ExamPage   ExamPage  { get; set; } = null!;
    public ExamOffer? Offer     { get; set; }
    public ExamAccess? Access   { get; set; }
    public ICollection<ExamOrderTransaction> Transactions { get; set; } = new List<ExamOrderTransaction>();
}
