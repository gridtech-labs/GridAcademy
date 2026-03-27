using GridAcademy.DTOs.Content.Masters;

namespace GridAcademy.Services;

public interface IMasterService
{
    // Question Types  (IDs are fixed — rename / activate only; no add/delete)
    Task<List<QuestionTypeDto>> GetQuestionTypesAsync(bool activeOnly = false);
    Task<QuestionTypeDto>       UpdateQuestionTypeAsync(int id, string name, string? description, bool isActive);

    // Subjects
    Task<List<SubjectDto>>       GetSubjectsAsync(bool activeOnly = true);
    Task<SubjectDto>             GetSubjectAsync(int id);
    Task<SubjectDto>             CreateSubjectAsync(CreateMasterRequest request);
    Task<SubjectDto>             UpdateSubjectAsync(int id, CreateMasterRequest request);
    Task                         DeleteSubjectAsync(int id);

    // Topics
    Task<List<TopicDto>>         GetTopicsAsync(int? subjectId = null, bool activeOnly = true);
    Task<TopicDto>               GetTopicAsync(int id);
    Task<TopicDto>               CreateTopicAsync(CreateTopicRequest request);
    Task<TopicDto>               UpdateTopicAsync(int id, CreateTopicRequest request);
    Task                         DeleteTopicAsync(int id);

    // Simple lookup lists (for dropdowns)
    Task<List<DifficultyLevelDto>> GetDifficultyLevelsAsync();
    Task<DifficultyLevelDto>       CreateDifficultyLevelAsync(CreateMasterRequest request);
    Task                           DeleteDifficultyLevelAsync(int id);

    Task<List<ComplexityLevelDto>> GetComplexityLevelsAsync();
    Task<ComplexityLevelDto>       CreateComplexityLevelAsync(CreateMasterRequest request);
    Task                           DeleteComplexityLevelAsync(int id);

    Task<List<ExamTypeDto>>        GetExamTypesAsync();
    Task<ExamTypeDto>              CreateExamTypeAsync(CreateMasterRequest request);
    Task                           DeleteExamTypeAsync(int id);

    Task<List<TagDto>>             GetTagsAsync();
    Task<TagDto>                   CreateTagAsync(CreateMasterRequest request);
    Task                           DeleteTagAsync(int id);

    Task<List<MarksDto>>           GetMarksAsync();
    Task<MarksDto>                 CreateMarksAsync(CreateMarksRequest request);
    Task                           DeleteMarksAsync(int id);

    Task<List<NegativeMarksDto>>   GetNegativeMarksAsync();
    Task<NegativeMarksDto>         CreateNegativeMarksAsync(CreateMarksRequest request);
    Task                           DeleteNegativeMarksAsync(int id);
}
