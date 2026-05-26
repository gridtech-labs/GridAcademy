namespace GridAcademy.Modules.AiGeneration.Domain.Entities;

public enum GenerationJobStatus
{
    Queued    = 1,
    Running   = 2,
    Completed = 3,
    Failed    = 4,
    Cancelled = 5
}

public enum DraftStatus
{
    PendingReview  = 1,
    Approved       = 2,
    Rejected       = 3,
    EditedApproved = 4
}

public enum EmbeddingSource
{
    Live  = 1,  // row in questions table
    Draft = 2   // row in question_drafts table
}

public enum QuestionReportStatus
{
    Open     = 1,
    Reviewed = 2,
    Resolved = 3,
    Dismissed = 4
}
