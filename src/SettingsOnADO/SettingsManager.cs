using SettingsOnADO.Utils;
using System.Data.Common;

namespace SettingsOnADO;

public class SettingsManager : ISettingsManager, ISettingsPubSub, IDisposable
{
    private readonly ISchemaManager schemaManager;
    private readonly IEncryptionProvider? encryptionProvider;
    private readonly ConcurrentTypeActionCollection subscribers = new();

    public SettingsManager(DbConnection connection, bool shouldCloseConnection = true, IEncryptionProvider? encryptionProvider = null)
        : this(new SchemaManager(connection, shouldCloseConnection), encryptionProvider) { }

    public SettingsManager(ISchemaManager schemaManager, IEncryptionProvider? encryptionProvider = null)
    {
        this.schemaManager = schemaManager ?? throw new ArgumentNullException(nameof(schemaManager));
        this.encryptionProvider = encryptionProvider;
    }

    public string DataSource => schemaManager.DataSource;

    public virtual TSettingsEntity Get<TSettingsEntity>() where TSettingsEntity : class, new()
    {
        var repository = new SettingsRepository(schemaManager, encryptionProvider);
        return repository.Get<TSettingsEntity>();
    }

    public virtual void Update<TSettingsEntity>(TSettingsEntity settings) where TSettingsEntity : class, new()
    {
        var repository = new SettingsRepository(schemaManager, encryptionProvider);
        var oldSettings = repository.Get<TSettingsEntity>();
        repository.Update(settings);
        OnSettingsChanged(oldSettings, settings);
    }

    public void Subscribe<TSettingsEntity>(Action<SettingsChangeEventArgs<TSettingsEntity>> handler) where TSettingsEntity : class, new()
    {
        subscribers.AddOrUpdate(handler);
    }

    public bool Unsubscribe<TSettingsEntity>(Action<SettingsChangeEventArgs<TSettingsEntity>> handler) where TSettingsEntity : class, new()
    {
        return subscribers.Remove(handler);
    }

    private void OnSettingsChanged<TSettingsEntity>(TSettingsEntity oldSettings, TSettingsEntity newSettings) where TSettingsEntity : class, new()
    {
        var actions = subscribers.GetActions<TSettingsEntity>();
        foreach (var action in actions)
        {
            action?.Invoke(new SettingsChangeEventArgs<TSettingsEntity>(oldSettings, newSettings));
        }
    }

    public void Dispose()
    {
        schemaManager?.Dispose();
    }
}