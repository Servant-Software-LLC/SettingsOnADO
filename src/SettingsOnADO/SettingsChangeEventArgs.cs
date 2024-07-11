namespace SettingsOnADO;

public class SettingsChangeEventArgs<TSettingsEntity> : EventArgs where TSettingsEntity : class, new()
{
    public TSettingsEntity OldSettings { get; }
    public TSettingsEntity NewSettings { get; }

    public SettingsChangeEventArgs(TSettingsEntity oldSettings, TSettingsEntity newSettings)
    {
        OldSettings = oldSettings;
        NewSettings = newSettings;
    }
}