using System.Data.Common;

namespace SettingsOnADO;

public class SettingsManager : ISettingsManager, IDisposable
{
    private readonly ISchemaManager schemaManager;

    public SettingsManager(DbConnection connection, bool shouldCloseConnection = true)
        : this(new SchemaManager(connection, shouldCloseConnection)) { }

    public SettingsManager(ISchemaManager schemaManager)
    {
        this.schemaManager = schemaManager ?? throw new ArgumentNullException(nameof(schemaManager));
    }

    public virtual TSettingsEntity Get<TSettingsEntity>() where TSettingsEntity : class, new()
    {
        var repository = new SettingsRepository(schemaManager);
        return repository.Get<TSettingsEntity>();
    }

    public virtual void Update<TSettingsEntity>(TSettingsEntity settings) where TSettingsEntity : class, new()
    {
        var repository = new SettingsRepository(schemaManager);
        repository.Update<TSettingsEntity>(settings);
    }

    public void Dispose()
    {
        schemaManager?.Dispose();
    }

}
