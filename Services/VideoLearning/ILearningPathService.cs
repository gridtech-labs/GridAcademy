using GridAcademy.DTOs.VideoLearning;

namespace GridAcademy.Services.VideoLearning;

public interface ILearningPathService
{
    Task<List<LearningPathDto>>    GetByDomainAsync(int domainId, bool activeOnly = true);
    Task<List<LearningPathDto>>    GetAllAsync(bool activeOnly = true);
    Task<LearningPathDto>          GetByIdAsync(Guid id);
    Task<LearningPathDetailDto>    GetDetailAsync(Guid id);
    Task<LearningPathDto>          CreateAsync(CreateLearningPathRequest request, IFormFile? thumbnail = null, Guid? createdBy = null);
    Task<LearningPathDto>          UpdateAsync(Guid id, UpdateLearningPathRequest request, IFormFile? thumbnail = null, Guid? updatedBy = null);
    Task                           DeleteAsync(Guid id);

    // ── Node management ──────────────────────────────────────────
    /// <summary>Add a Module node (NodeType = "N") to the learning path.</summary>
    Task<LpNodeDto>                AddModuleAsync(Guid learningPathId, CreateLpModuleRequest request);

    /// <summary>Add one content node (AS/VL/SC/PD/HT) under an optional parent module.</summary>
    Task<LpNodeDto>                AddContentNodeAsync(Guid learningPathId, CreateLpContentRequest request);

    /// <summary>Batch-add multiple content nodes of the same type (e.g. multi-selected assessments).</summary>
    Task<List<LpNodeDto>>          AddContentBatchAsync(Guid learningPathId, AddLpContentBatchRequest request);

    Task                           DeleteNodeAsync(int nodeId);
    Task                           ReorderNodesAsync(Guid learningPathId, int? parentNodeId, List<(int NodeId, int SortOrder)> items);
}
