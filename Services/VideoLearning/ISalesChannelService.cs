using GridAcademy.Data.Entities.VideoLearning;
using GridAcademy.DTOs.VideoLearning;

namespace GridAcademy.Services.VideoLearning;

public interface ISalesChannelService
{
    Task<List<SalesChannelDto>>   GetAllAsync();
    Task<SalesChannelDto>         GetByIdAsync(int id);
    Task<CreateChannelResult>     CreateAsync(CreateSalesChannelRequest request);
    Task<SalesChannelDto>         UpdateAsync(int id, UpdateSalesChannelRequest request);
    Task                          DeleteAsync(int id);
    Task<VlSalesChannel?>         AuthenticateAsync(string rawApiKey);
    Task<ChannelPriceDto>         SetPriceOverrideAsync(int channelId, int planId, SetChannelPriceRequest request);
    Task                          RemovePriceOverrideAsync(int channelId, int planId);
    Task<List<ChannelPriceDto>>   GetPriceOverridesAsync(int channelId);
}
