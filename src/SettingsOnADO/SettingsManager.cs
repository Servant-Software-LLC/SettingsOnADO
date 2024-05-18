using System.Data.Common;

namespace SettingsOnADO;

public class SettingsManager : ISettingsManager, IDisposable
{
    private readonly ISchemaManager schemaManager;
    private readonly IEncryptionProvider? encryptionProvider;

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
        repository.Update<TSettingsEntity>(settings);
    }

    public void Dispose()
    {
        schemaManager?.Dispose();
    }

}
