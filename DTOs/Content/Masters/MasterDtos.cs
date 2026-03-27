namespace GridAcademy.DTOs.Content.Masters;

// Generic response for all master tables
public record MasterDto(int Id, string Name, bool IsActive, int SortOrder);

/// <summary>Question type master entry (id matches QuestionType enum value).</summary>
public record QuestionTypeDto(int Id, string Name, string Code, string? Description, bool IsActive, int SortOrder);

// Typed wrappers for swagger discriminability
public record SubjectDto(int Id, string Name, bool IsActive, int SortOrder) : MasterDto(Id, Name, IsActive, SortOrder);
public record TopicDto(int Id, string Name, bool IsActive, int SortOrder, int SubjectId, string SubjectName)
    : MasterDto(Id, Name, IsActive, SortOrder);

public record DifficultyLevelDto(int Id, string Name, bool IsActive, int SortOrder) : MasterDto(Id, Name, IsActive, SortOrder);
public record ComplexityLevelDto(int Id, string Name, bool IsActive, int SortOrder) : MasterDto(Id, Name, IsActive, SortOrder);
public record ExamTypeDto      (int Id, string Name, bool IsActive, int SortOrder) : MasterDto(Id, Name, IsActive, SortOrder);
public record TagDto           (int Id, string Name, bool IsActive, int SortOrder) : MasterDto(Id, Name, IsActive, SortOrder);
public record MarksDto         (int Id, string Name, bool IsActive, int SortOrder, decimal Value) : MasterDto(Id, Name, IsActive, SortOrder);
public record NegativeMarksDto (int Id, string Name, bool IsActive, int SortOrder, decimal Value) : MasterDto(Id, Name, IsActive, SortOrder);

// Create / update requests
public record CreateMasterRequest(string Name, bool IsActive = true, int SortOrder = 0);
public record CreateTopicRequest(string Name, int SubjectId, bool IsActive = true, int SortOrder = 0);
public record CreateMarksRequest(string Name, decimal Value, bool IsActive = true, int SortOrder = 0);

// Question type update — rename / toggle only; ID and Code are immutable
public record UpdateQuestionTypeRequest(string Name, string? Description, bool IsActive = true);
