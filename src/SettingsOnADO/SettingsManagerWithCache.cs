using SettingsOnADO.Utils;

namespace SettingsOnADO;

public class SettingsManagerWithCache : ISettingsManagerWithCache, ISettingsManager
{
    private readonly ISettingsManager innerSettingsManager;
    private readonly Dictionary<string, object> settingsCache = new Dictionary<string, object>();
    private readonly SettingsPubSub settingsPubSub = new();

    public SettingsManagerWithCache(ISettingsManager innerSettingsManager)
    {
        this.innerSettingsManager = innerSettingsManager ?? throw new ArgumentNullException(nameof(innerSettingsManager));
    }

    public void SetCacheValue<TSettingsEntity>(TSettingsEntity settings) where TSettingsEntity : class, new()
    {
        var oldSettings = innerSettingsManager.Get<TSettingsEntity>();
        settingsCache[GetCacheKey<TSettingsEntity>()] = settings;
        settingsPubSub.Notify(oldSettings, settings);
    }

    public bool RemoveCacheValue<TSettingsEntity>() where TSettingsEntity : class
    {
        return settingsCache.Remove(GetCacheKey<TSettingsEntity>());
    }

    public string DataSource => innerSettingsManager.DataSource;
    
    public TSettingsEntity Get<TSettingsEntity>() where TSettingsEntity : class, new()
    {
        // Check cache first
        if (settingsCache.TryGetValue(GetCacheKey<TSettingsEntity>(), out var cachedValue) && cachedValue is TSettingsEntity typedValue)
        {
            return typedValue;
        }

        // Fallback to the inner settings manager
        return innerSettingsManager.Get<TSettingsEntity>();
    }

    public void Update<TSettingsEntity>(TSettingsEntity settings) where TSettingsEntity : class, new()
    {
        var oldSettings = Get<TSettingsEntity>();

        //If the cache contains the settings, remove the cached value
        RemoveCacheValue<TSettingsEntity>();

        innerSettingsManager.Update(settings);
        settingsPubSub.Notify(oldSettings, settings);
    }

    public void Subscribe<TSettingsEntity>(Action<SettingsChangeEventArgs<TSettingsEntity>> handler) where TSettingsEntity : class, new() =>
        settingsPubSub.Subscribe(handler);

    public bool Unsubscribe<TSettingsEntity>(Action<SettingsChangeEventArgs<TSettingsEntity>> handler) where TSettingsEntity : class, new() =>
        settingsPubSub.Unsubscribe(handler);

    public void Dispose()
    {
        innerSettingsManager.Dispose();
    }

    private string GetCacheKey<TSettingsEntity>() where TSettingsEntity : class =>
        typeof(TSettingsEntity).Name;
}
