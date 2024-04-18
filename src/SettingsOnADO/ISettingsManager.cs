namespace SettingsOnADO;

public interface ISettingsManager : IDisposable
{
    TSettingsEntity Get<TSettingsEntity>()
        where TSettingsEntity : class, new();
    void Update<TSettingsEntity>(TSettingsEntity settings)
        where TSettingsEntity : class, new();
}
