namespace GridAcademy.Data.Entities.Marketplace;

public enum ProviderStatus   { Pending = 0, Verified = 1, Suspended = 2 }
public enum SeriesStatus     { Draft = 0, PendingReview = 1, Published = 2, Rejected = 3 }
public enum SeriesType       { FullMock = 0, Sectional = 1, PreviousYear = 2, MiniMock = 3 }
public enum OrderStatus      { Pending = 0, Paid = 1, Failed = 2, Refunded = 3 }
public enum CommissionStatus { Pending = 0, Processed = 1 }
public enum PayoutStatus     { Initiated = 0, Success = 1, Failed = 2 }
public enum DiscountType     { Flat = 0, Percentage = 1 }
