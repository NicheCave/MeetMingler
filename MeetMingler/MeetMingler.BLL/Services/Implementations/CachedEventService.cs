using MeetMingler.BLL.Caching;
using MeetMingler.BLL.Filters;
using MeetMingler.BLL.Models;
using MeetMingler.BLL.Models.Event;
using MeetMingler.BLL.Models.User;
using MeetMingler.BLL.Services.Contracts;
using Microsoft.Extensions.Caching.Memory;

namespace MeetMingler.BLL.Services.Implementations;

public class CachedEventService(
    EventService inner,
    IMemoryCache cache,
    CacheSignals signals,
    ICurrentUser currentUser) : IEventService
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(5);

    public async Task<EventVM?> CreateAsync(EventIM im, CancellationToken cf = default)
    {
        var result = await inner.CreateAsync(im, cf);
        Invalidate();
        return result;
    }

    public Task<EventExtendedVM?> GetByIdAsync(Guid id, CancellationToken cf = default)
    {
        var key = $"{CacheKeys.Event(id)}:{currentUser.User.Id}";
        return GetOrLoadAsync(key, () => inner.GetByIdAsync(id, cf), CacheKeys.Event(id));
    }

    public async Task<IEnumerable<EventVM>> GetCollectionByCreatorAsync(Guid creatorId,
        List<string> includeMetadataKeys,
        PaginationOptions pagination,
        CancellationToken cf = default)
    {
        var key = CacheKeys.Of("events:creator", new { creatorId, includeMetadataKeys, pagination });
        return await GetOrLoadAsync(key,
            async () => (await inner.GetCollectionByCreatorAsync(creatorId, includeMetadataKeys, pagination, cf))
                .ToList(),
            CacheKeys.Events) ?? [];
    }

    public IQueryable<string> GetDistinctMetadataValuesAsync(string metadataKey, CancellationToken cf = default)
    {
        return inner.GetDistinctMetadataValuesAsync(metadataKey, cf);
    }

    public async Task<BaseCollectionVM<EventVM>> GetAllPaginatedAndFilteredAsync(PaginationOptions paginationOptions,
        EventFilter filter, CancellationToken cf = default)
    {
        var key = CacheKeys.Of("events:filtered", new { paginationOptions, filter });
        var result = await GetOrLoadAsync(key, async () =>
        {
            var page = await inner.GetAllPaginatedAndFilteredAsync(paginationOptions, filter, cf);
            return new BaseCollectionVM<EventVM> { Items = page.Items.ToList(), Count = page.Count };
        }, CacheKeys.Events);

        return result!;
    }

    public async Task<bool> DeleteAsync(Guid eventId, CancellationToken cf = default)
    {
        var deleted = await inner.DeleteAsync(eventId, cf);
        if (deleted)
            Invalidate(eventId);

        return deleted;
    }

    public async Task<EventVM?> AddMetadataAsync(Guid eventId, EventMetadataIM metadata, CancellationToken cf = default)
    {
        var result = await inner.AddMetadataAsync(eventId, metadata, cf);
        if (result != null)
            Invalidate(eventId);

        return result;
    }

    public async Task<EventVM?> DeleteMetadataAsync(Guid eventId, string key, CancellationToken cf = default)
    {
        var result = await inner.DeleteMetadataAsync(eventId, key, cf);
        if (result != null)
            Invalidate(eventId);

        return result;
    }

    public async Task<bool> SetCancelledAsync(Guid id, bool cancelled, CancellationToken cf = default)
    {
        var updated = await inner.SetCancelledAsync(id, cancelled, cf);
        if (updated)
            Invalidate(id);

        return updated;
    }

    public async Task<bool> RegisterUserForEventAsync(Guid eventId, CancellationToken cf = default)
    {
        var registered = await inner.RegisterUserForEventAsync(eventId, cf);
        if (registered)
            Invalidate(eventId);

        return registered;
    }

    public async Task<IEnumerable<DateTime>> GetDatesAsync(DateTime startDateRange, DateTime endDateRange,
        CancellationToken cf = default)
    {
        var key = CacheKeys.Of("events:dates", new { startDateRange, endDateRange });
        return await GetOrLoadAsync(key,
            async () => (await inner.GetDatesAsync(startDateRange, endDateRange, cf)).ToList(),
            CacheKeys.Events) ?? [];
    }

    public async Task<EventVM?> UpdateAsync(Guid eventId, EventUM updateModel, CancellationToken cf = default)
    {
        var result = await inner.UpdateAsync(eventId, updateModel, cf);
        if (result != null)
            Invalidate(eventId);

        return result;
    }

    public Task<BaseCollectionVM<UserVM>?> GetAttendees(Guid eventId, PaginationOptions pagination,
        CancellationToken cf = default)
    {
        var key = CacheKeys.Of($"{CacheKeys.Event(eventId)}:attendees", pagination);
        return GetOrLoadAsync(key, () => inner.GetAttendees(eventId, pagination, cf), CacheKeys.Event(eventId));
    }

    public Task<BaseCollectionVM<EventVM>?> GetCurrentUserAttendance(PaginationOptions pagination,
        CancellationToken cf = default)
    {
        var key = CacheKeys.Of($"{CacheKeys.User(currentUser.User.Id)}:attendance", pagination);
        return GetOrLoadAsync(key, () => inner.GetCurrentUserAttendance(pagination, cf), CacheKeys.Events);
    }

    private async Task<T?> GetOrLoadAsync<T>(string key, Func<Task<T?>> load, params string[] dependsOn)
        where T : class
    {
        if (cache.TryGetValue(key, out T? cached))
            return cached;

        var options = new MemoryCacheEntryOptions().SetAbsoluteExpiration(Ttl);
        foreach (var signal in dependsOn)
            options.AddExpirationToken(signals.GetToken(signal));

        var value = await load();
        if (value != null)
            cache.Set(key, value, options);

        return value;
    }

    private void Invalidate(Guid? eventId = null)
    {
        signals.Signal(CacheKeys.Events);
        if (eventId != null)
            signals.Signal(CacheKeys.Event(eventId.Value));
    }
}
