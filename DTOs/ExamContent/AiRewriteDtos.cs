namespace GridAcademy.DTOs.ExamContent;

public record AiRewriteRequest(int? BatchSize);

public record AiRewriteBatchResponse(int Processed, int Failed);
