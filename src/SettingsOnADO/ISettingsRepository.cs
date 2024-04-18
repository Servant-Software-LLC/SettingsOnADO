namespace SettingsOnADO;

public interface ISettingsRepository
{
    TSettingsEntity Get<TSettingsEntity>()
        where TSettingsEntity : class, new();
    void Update<TSettingsEntity>(TSettingsEntity settings)
        where TSettingsEntity : class, new();
}
