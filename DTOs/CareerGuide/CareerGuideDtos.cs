namespace GridAcademy.DTOs.CareerGuide;

public record CareerQuizOptionDto(int Id, string OptionText, string CareerCategory, int SortOrder);

public record CareerQuizQuestionDto(
    int Id,
    string QuestionText,
    int SortOrder,
    bool IsActive,
    IReadOnlyList<CareerQuizOptionDto> Options);

public record CreateOptionRequest(string OptionText, string CareerCategory, int SortOrder);

public record CreateQuizQuestionRequest(
    string QuestionText,
    int SortOrder,
    bool IsActive,
    List<CreateOptionRequest> Options);

public record UpdateQuizQuestionRequest(
    string QuestionText,
    int SortOrder,
    bool IsActive,
    List<CreateOptionRequest> Options);
