namespace SettingsOnADO;

public interface ISettingsPubSub
{
    void Subscribe<TSettingsEntity>(Action<SettingsChangeEventArgs<TSettingsEntity>> handler)
        where TSettingsEntity : class, new();

    bool Unsubscribe<TSettingsEntity>(Action<SettingsChangeEventArgs<TSettingsEntity>> handler)
        where TSettingsEntity : class, new();
}