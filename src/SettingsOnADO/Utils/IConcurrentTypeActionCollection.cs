namespace SettingsOnADO.Utils;

interface IConcurrentTypeActionCollection
{
    void AddOrUpdate<TSettingsEntity>(Action<SettingsChangeEventArgs<TSettingsEntity>> action)
        where TSettingsEntity : class, new();

    bool Remove<TSettingsEntity>(Action<SettingsChangeEventArgs<TSettingsEntity>> action)
        where TSettingsEntity : class, new();

    IEnumerable<Action<SettingsChangeEventArgs<TSettingsEntity>>> GetActions<TSettingsEntity>()
        where TSettingsEntity : class, new ();
}
