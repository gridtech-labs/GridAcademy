using System.Security.Cryptography;
using System.Text;
using GridAcademy.Data;
using GridAcademy.Data.Entities.VideoLearning;
using GridAcademy.DTOs.VideoLearning;
using Microsoft.EntityFrameworkCore;

namespace GridAcademy.Services.VideoLearning;

public class SalesChannelService(AppDbContext db) : ISalesChannelService
{
    private static string HashKey(string key) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key))).ToLowerInvariant();

    private static SalesChannelDto Map(VlSalesChannel c) =>
        new(c.Id, c.Name, c.BaseUrl, c.IsActive, c.CreatedAt);

    public async Task<List<SalesChannelDto>> GetAllAsync() =>
        (await db.VlSalesChannels.OrderBy(c => c.Name).ToListAsync()).Select(Map).ToList();

    public async Task<SalesChannelDto> GetByIdAsync(int id)
    {
        var c = await db.VlSalesChannels.FindAsync(id)
            ?? throw new KeyNotFoundException($"Channel {id} not found.");
        return Map(c);
    }

    public async Task<CreateChannelResult> CreateAsync(CreateSalesChannelRequest request)
    {
        var rawKey = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"); // 64-char random key
        var entity = new VlSalesChannel {
            Name = request.Name, BaseUrl = request.BaseUrl, IsActive = request.IsActive,
            ApiKeyHash = HashKey(rawKey), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.VlSalesChannels.Add(entity);
        await db.SaveChangesAsync();
        return new CreateChannelResult(Map(entity), rawKey);
    }

    public async Task<SalesChannelDto> UpdateAsync(int id, UpdateSalesChannelRequest request)
    {
        var entity = await db.VlSalesChannels.FindAsync(id)
            ?? throw new KeyNotFoundException($"Channel {id} not found.");
        entity.Name = request.Name; entity.BaseUrl = request.BaseUrl;
        entity.IsActive = request.IsActive; entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Map(entity);
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await db.VlSalesChannels.FindAsync(id)
            ?? throw new KeyNotFoundException($"Channel {id} not found.");
        db.VlSalesChannels.Remove(entity);
        await db.SaveChangesAsync();
    }

    public async Task<VlSalesChannel?> AuthenticateAsync(string rawApiKey)
    {
        var hash = HashKey(rawApiKey);
        return await db.VlSalesChannels.FirstOrDefaultAsync(c => c.ApiKeyHash == hash && c.IsActive);
    }

    public async Task<ChannelPriceDto> SetPriceOverrideAsync(int channelId, int planId, SetChannelPriceRequest request)
    {
        var existing = await db.VlChannelProgramPrices
            .FirstOrDefaultAsync(x => x.ChannelId == channelId && x.PricingPlanId == planId);
        if (existing == null)
        {
            existing = new VlChannelProgramPrice { ChannelId = channelId, PricingPlanId = planId };
            db.VlChannelProgramPrices.Add(existing);
        }
        existing.OverridePriceInr = request.OverridePriceInr;
        existing.OverridePriceUsd = request.OverridePriceUsd;
        existing.IsActive = request.IsActive;
        await db.SaveChangesAsync();
        return await BuildChannelPriceDtoAsync(existing);
    }

    public async Task RemovePriceOverrideAsync(int channelId, int planId)
    {
        var item = await db.VlChannelProgramPrices
            .FirstOrDefaultAsync(x => x.ChannelId == channelId && x.PricingPlanId == planId);
        if (item == null) return;
        db.VlChannelProgramPrices.Remove(item);
        await db.SaveChangesAsync();
    }

    public async Task<List<ChannelPriceDto>> GetPriceOverridesAsync(int channelId)
    {
        var items = await db.VlChannelProgramPrices
            .Include(x => x.PricingPlan).ThenInclude(pp => pp.Program)
            .Where(x => x.ChannelId == channelId).ToListAsync();
        return items.Select(x => new ChannelPriceDto(
            x.Id, x.ChannelId, x.PricingPlanId, x.PricingPlan.Name,
            x.PricingPlan.Program?.Title ?? "",
            x.PricingPlan.PriceInr, x.PricingPlan.PriceUsd,
            x.OverridePriceInr, x.OverridePriceUsd, x.IsActive)).ToList();
    }

    private async Task<ChannelPriceDto> BuildChannelPriceDtoAsync(VlChannelProgramPrice x)
    {
        await db.Entry(x).Reference(e => e.PricingPlan).LoadAsync();
        await db.Entry(x.PricingPlan).Reference(pp => pp.Program).LoadAsync();
        return new ChannelPriceDto(x.Id, x.ChannelId, x.PricingPlanId, x.PricingPlan.Name,
            x.PricingPlan.Program?.Title ?? "",
            x.PricingPlan.PriceInr, x.PricingPlan.PriceUsd,
            x.OverridePriceInr, x.OverridePriceUsd, x.IsActive);
    }
}
