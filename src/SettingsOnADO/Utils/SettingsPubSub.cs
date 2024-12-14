namespace SettingsOnADO.Utils;

internal class SettingsPubSub
{
    private readonly ConcurrentTypeActionCollection subscribers = new();

    public void Subscribe<TSettingsEntity>(Action<SettingsChangeEventArgs<TSettingsEntity>> handler) where TSettingsEntity : class, new() =>
        subscribers.AddOrUpdate(handler);

    public bool Unsubscribe<TSettingsEntity>(Action<SettingsChangeEventArgs<TSettingsEntity>> handler) where TSettingsEntity : class, new() => 
        subscribers.Remove(handler);

    public void Notify<TSettingsEntity>(TSettingsEntity oldSettings, TSettingsEntity newSettings) where TSettingsEntity : class, new()
    {
        var actions = subscribers.GetActions<TSettingsEntity>();
        foreach (var action in actions)
        {
            action?.Invoke(new SettingsChangeEventArgs<TSettingsEntity>(oldSettings, newSettings));
        }
    }
}
