using GridAcademy.Data.Entities.Content;

namespace GridAcademy.Data.Entities.Exam;

/// <summary>Exam level master: All India Level, State Level, School Exam, University Exam, etc.</summary>
public class ExamLevel : MasterBase
{
    public ICollection<ExamPage> ExamPages { get; set; } = [];
}
