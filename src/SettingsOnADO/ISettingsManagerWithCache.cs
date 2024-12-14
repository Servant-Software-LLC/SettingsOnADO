namespace SettingsOnADO;

public interface ISettingsManagerWithCache : ISettingsManager
{
    void SetCacheValue<TSettingsEntity>(TSettingsEntity settings)
        where TSettingsEntity : class, new();
    bool RemoveCacheValue<TSettingsEntity>()
        where TSettingsEntity : class;
}
