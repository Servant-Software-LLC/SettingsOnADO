namespace SettingsOnADO;

public interface ISettingsManager : IDisposable
{
    string DataSource { get; }

    TSettingsEntity Get<TSettingsEntity>()
        where TSettingsEntity : class, new();
    void Update<TSettingsEntity>(TSettingsEntity settings)
        where TSettingsEntity : class, new();
}
