using SettingsOnADO.Utils;
using System.Data.Common;

namespace SettingsOnADO;

public class SettingsManager : ISettingsManager, ISettingsPubSub, IDisposable
{
    private readonly ISettingsRepository settingsRepository;
    private readonly ISchemaManager? schemaManager;
    private readonly SettingsPubSub settingsPubSub = new();

    public SettingsManager(DbConnection connection, bool shouldCloseConnection = true, IEncryptionProvider? encryptionProvider = null)
        : this(new SchemaManager(connection, shouldCloseConnection), encryptionProvider) { }

    public SettingsManager(ISchemaManager schemaManager, IEncryptionProvider? encryptionProvider = null)
        : this(new SettingsRepository(schemaManager, encryptionProvider)) 
    {
        //Save a reference for proper disposal.
        this.schemaManager = schemaManager ?? throw new ArgumentNullException(nameof(schemaManager));
    }
    
    public SettingsManager(ISettingsRepository settingsRepository)
    {
        this.settingsRepository = settingsRepository ?? throw new ArgumentNullException(nameof(settingsRepository));
    }

    public string DataSource => schemaManager == null ? "<None>" : schemaManager.DataSource;

    public virtual TSettingsEntity Get<TSettingsEntity>() where TSettingsEntity : class, new() => 
        settingsRepository.Get<TSettingsEntity>();

    public virtual void Update<TSettingsEntity>(TSettingsEntity settings) where TSettingsEntity : class, new()
    {
        var oldSettings = settingsRepository.Get<TSettingsEntity>();
        settingsRepository.Update(settings);
        settingsPubSub.Notify(oldSettings, settings);
    }

    public void Subscribe<TSettingsEntity>(Action<SettingsChangeEventArgs<TSettingsEntity>> handler) where TSettingsEntity : class, new() =>
        settingsPubSub.Subscribe(handler);

    public bool Unsubscribe<TSettingsEntity>(Action<SettingsChangeEventArgs<TSettingsEntity>> handler) where TSettingsEntity : class, new() =>
        settingsPubSub.Unsubscribe(handler);

    public void Dispose()
    {
        schemaManager?.Dispose();
    }
}