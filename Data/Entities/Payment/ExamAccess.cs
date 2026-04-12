using GridAcademy.Data.Entities;
using GridAcademy.Data.Entities.Exam;

namespace GridAcademy.Data.Entities.Payment;

/// <summary>Grants a student access to an exam page after successful payment (or manual grant).</summary>
public class ExamAccess
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid StudentId  { get; set; }
    public Guid ExamPageId { get; set; }
    public Guid OrderId    { get; set; }

    public DateTime  GrantedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }   // null = lifetime access
    public bool      IsActive  { get; set; } = true;

    // Navigation
    public User      Student  { get; set; } = null!;
    public ExamPage  ExamPage { get; set; } = null!;
    public ExamOrder Order    { get; set; } = null!;
}
