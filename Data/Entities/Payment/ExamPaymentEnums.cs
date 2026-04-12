namespace GridAcademy.Data.Entities.Payment;

public enum ExamOrderStatus
{
    Initiated = 0,   // Order record created in our DB
    Pending   = 1,   // Razorpay order created, awaiting payment from student
    Captured  = 2,   // Payment successful & verified
    Failed    = 3,   // Payment failed / signature mismatch
    Refunded  = 4,   // Amount refunded to student
    Expired   = 5    // Order expired before payment
}

public enum ExamOfferType
{
    PercentageDiscount = 0,   // e.g. 20% off
    FixedDiscount      = 1,   // e.g. ₹50 off
    FreeAccess         = 2    // 100% free (no payment needed)
}
