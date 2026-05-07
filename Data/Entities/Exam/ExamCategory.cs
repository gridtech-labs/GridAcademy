using GridAcademy.Data.Entities.Content;

namespace GridAcademy.Data.Entities.Exam;

public class ExamCategory : MasterBase
{
    public ICollection<ExamSubCategory> SubCategories { get; set; } = [];
}

public class ExamSubCategory : MasterBase
{
    public int ExamCategoryId { get; set; }

    public ExamCategory Category { get; set; } = null!;
}
