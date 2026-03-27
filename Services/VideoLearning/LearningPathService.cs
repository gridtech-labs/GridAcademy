using GridAcademy.Data;
using GridAcademy.Data.Entities.VideoLearning;
using GridAcademy.DTOs.VideoLearning;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GridAcademy.Services.VideoLearning;

public class LearningPathService(AppDbContext db, IWebHostEnvironment env,
    IOptions<VideoLearningStorageOptions> opts) : ILearningPathService
{
    private readonly VideoLearningStorageOptions _opts = opts.Value;

    // ── Mappers ────────────────────────────────────────────────────────────

    private static LpNodeDto MapNode(VlLearningPathNode n) => new(
        n.Id, n.LearningPathId, n.ParentNodeId,
        n.NodeType, n.Title, n.ContentId,
        n.IsPreview, n.SortOrder, n.IsActive,
        n.ChildNodes.OrderBy(c => c.SortOrder).Select(MapNode).ToList());

    private static LearningPathDto MapSummary(VlLearningPath lp)
    {
        var moduleCount  = lp.Nodes.Count(n => n.NodeType == LpNodeType.Module);
        var nodeCount    = lp.Nodes.Count;
        return new LearningPathDto(
            lp.Id, lp.DomainId, lp.Domain?.Name ?? "", lp.Title, lp.Description,
            lp.ThumbnailPath, lp.IsActive, lp.SortOrder,
            nodeCount, moduleCount, lp.CreatedAt);
    }

    private static LearningPathDetailDto MapDetail(VlLearningPath lp)
    {
        // Only top-level nodes (no parent) — children are nested via ChildNodes
        var topNodes = lp.Nodes
            .Where(n => n.ParentNodeId == null)
            .OrderBy(n => n.SortOrder)
            .Select(MapNode)
            .ToList();
        return new LearningPathDetailDto(
            lp.Id, lp.DomainId, lp.Domain?.Name ?? "", lp.Title, lp.Description,
            lp.ThumbnailPath, lp.IsActive, lp.SortOrder,
            lp.Nodes.Count, lp.CreatedAt, topNodes);
    }

    // ── Queries ────────────────────────────────────────────────────────────

    private IQueryable<VlLearningPath> BaseQuery() =>
        db.VlLearningPaths
            .Include(lp => lp.Domain)
            .Include(lp => lp.Nodes).ThenInclude(n => n.ChildNodes);

    public async Task<List<LearningPathDto>> GetAllAsync(bool activeOnly = true)
    {
        var q = BaseQuery().AsQueryable();
        if (activeOnly) q = q.Where(lp => lp.IsActive);
        return (await q.OrderBy(lp => lp.DomainId).ThenBy(lp => lp.SortOrder).ToListAsync())
            .Select(MapSummary).ToList();
    }

    public async Task<List<LearningPathDto>> GetByDomainAsync(int domainId, bool activeOnly = true)
    {
        var q = BaseQuery().Where(lp => lp.DomainId == domainId);
        if (activeOnly) q = q.Where(lp => lp.IsActive);
        return (await q.OrderBy(lp => lp.SortOrder).ToListAsync()).Select(MapSummary).ToList();
    }

    public async Task<LearningPathDto> GetByIdAsync(Guid id)
    {
        var lp = await BaseQuery().FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException($"Learning path {id} not found.");
        return MapSummary(lp);
    }

    public async Task<LearningPathDetailDto> GetDetailAsync(Guid id)
    {
        var lp = await BaseQuery().FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException($"Learning path {id} not found.");
        return MapDetail(lp);
    }

    // ── CRUD ───────────────────────────────────────────────────────────────

    public async Task<LearningPathDto> CreateAsync(CreateLearningPathRequest request,
        IFormFile? thumbnail = null, Guid? createdBy = null)
    {
        var entity = new VlLearningPath
        {
            DomainId = request.DomainId, Title = request.Title,
            Description = request.Description,
            IsActive = request.IsActive, SortOrder = request.SortOrder,
            CreatedBy = createdBy, UpdatedBy = createdBy,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        if (thumbnail != null) entity.ThumbnailPath = await SaveThumbnailAsync(entity.Id, thumbnail);
        db.VlLearningPaths.Add(entity);
        await db.SaveChangesAsync();
        return await GetByIdAsync(entity.Id);
    }

    public async Task<LearningPathDto> UpdateAsync(Guid id, UpdateLearningPathRequest request,
        IFormFile? thumbnail = null, Guid? updatedBy = null)
    {
        var entity = await db.VlLearningPaths.FindAsync(id)
            ?? throw new KeyNotFoundException($"Learning path {id} not found.");
        entity.Title = request.Title; entity.Description = request.Description;
        entity.IsActive = request.IsActive; entity.SortOrder = request.SortOrder;
        entity.UpdatedBy = updatedBy; entity.UpdatedAt = DateTime.UtcNow;
        if (thumbnail != null) entity.ThumbnailPath = await SaveThumbnailAsync(id, thumbnail);
        await db.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await db.VlLearningPaths.FindAsync(id)
            ?? throw new KeyNotFoundException($"Learning path {id} not found.");
        db.VlLearningPaths.Remove(entity);
        await db.SaveChangesAsync();
    }

    // ── Node management ────────────────────────────────────────────────────

    public async Task<LpNodeDto> AddModuleAsync(Guid learningPathId, CreateLpModuleRequest request)
    {
        var lp = await db.VlLearningPaths.FindAsync(learningPathId)
            ?? throw new KeyNotFoundException($"Learning path {learningPathId} not found.");

        // Next sort order = max existing top-level + 10
        var nextSort = await db.VlLearningPathNodes
            .Where(n => n.LearningPathId == learningPathId && n.ParentNodeId == null)
            .MaxAsync(n => (int?)n.SortOrder) ?? 0;

        var node = new VlLearningPathNode
        {
            LearningPathId = learningPathId,
            ParentNodeId = null,
            NodeType = LpNodeType.Module,
            Title = request.Title,
            SortOrder = request.SortOrder > 0 ? request.SortOrder : nextSort + 10,
            IsActive = true,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        db.VlLearningPathNodes.Add(node);
        await db.SaveChangesAsync();
        node.ChildNodes = [];
        return MapNode(node);
    }

    public async Task<LpNodeDto> AddContentNodeAsync(Guid learningPathId, CreateLpContentRequest request)
    {
        var nextSort = await db.VlLearningPathNodes
            .Where(n => n.LearningPathId == learningPathId && n.ParentNodeId == request.ParentNodeId)
            .MaxAsync(n => (int?)n.SortOrder) ?? 0;

        var node = new VlLearningPathNode
        {
            LearningPathId = learningPathId,
            ParentNodeId = request.ParentNodeId,
            NodeType = request.NodeType,
            Title = request.Title,
            ContentId = request.ContentId,
            IsPreview = request.IsPreview,
            SortOrder = request.SortOrder > 0 ? request.SortOrder : nextSort + 10,
            IsActive = true,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        db.VlLearningPathNodes.Add(node);
        await db.SaveChangesAsync();
        node.ChildNodes = [];
        return MapNode(node);
    }

    public async Task<List<LpNodeDto>> AddContentBatchAsync(Guid learningPathId, AddLpContentBatchRequest request)
    {
        var nextSort = await db.VlLearningPathNodes
            .Where(n => n.LearningPathId == learningPathId && n.ParentNodeId == request.ParentNodeId)
            .MaxAsync(n => (int?)n.SortOrder) ?? 0;

        var nodes = new List<VlLearningPathNode>();
        foreach (var contentId in request.ContentIds)
        {
            nextSort += 10;
            // Resolve display title from the content record
            var title = await ResolveTitleAsync(request.NodeType, contentId);
            var node = new VlLearningPathNode
            {
                LearningPathId = learningPathId,
                ParentNodeId = request.ParentNodeId,
                NodeType = request.NodeType,
                Title = title,
                ContentId = contentId,
                IsPreview = request.IsPreview,
                SortOrder = nextSort,
                IsActive = true,
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
            };
            nodes.Add(node);
        }
        db.VlLearningPathNodes.AddRange(nodes);
        await db.SaveChangesAsync();
        return nodes.Select(n => { n.ChildNodes = []; return MapNode(n); }).ToList();
    }

    public async Task DeleteNodeAsync(int nodeId)
    {
        var node = await db.VlLearningPathNodes.FindAsync(nodeId)
            ?? throw new KeyNotFoundException($"Node {nodeId} not found.");
        // EF cascade will delete child nodes (content under a module)
        db.VlLearningPathNodes.Remove(node);
        await db.SaveChangesAsync();
    }

    public async Task ReorderNodesAsync(Guid learningPathId, int? parentNodeId, List<(int NodeId, int SortOrder)> items)
    {
        foreach (var (nodeId, sortOrder) in items)
        {
            var node = await db.VlLearningPathNodes.FindAsync(nodeId);
            if (node != null && node.LearningPathId == learningPathId && node.ParentNodeId == parentNodeId)
                node.SortOrder = sortOrder;
        }
        await db.SaveChangesAsync();
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private async Task<string> ResolveTitleAsync(string nodeType, Guid contentId) => nodeType switch
    {
        LpNodeType.Video      => (await db.VlVideos.FindAsync(contentId))?.Title ?? "Video",
        LpNodeType.Assessment => (await db.Tests.FindAsync(contentId))?.Title ?? "Assessment",
        LpNodeType.Scorm or LpNodeType.Html or LpNodeType.Pdf
                              => (await db.VlContentFiles.FindAsync(contentId))?.Title ?? LpNodeType.Label(nodeType),
        _                     => "Content"
    };

    private async Task<string> SaveThumbnailAsync(Guid id, IFormFile file)
    {
        var dir = Path.Combine(env.WebRootPath, _opts.ThumbnailUploadPath, "lp");
        Directory.CreateDirectory(dir);
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileName = $"{id}{ext}";
        await using var stream = new FileStream(Path.Combine(dir, fileName), FileMode.Create);
        await file.CopyToAsync(stream);
        return $"/{_opts.ThumbnailUploadPath}/lp/{fileName}";
    }
}
