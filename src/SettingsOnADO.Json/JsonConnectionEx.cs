using Data.Common.FileStatements;
using Data.Common.Interfaces;
using Data.Common.Utils.ConnectionString;
using System.Data.JsonClient;

namespace SettingsOnADO.Json;

public class JsonConnectionEx : JsonConnection
{
    public readonly Dictionary<string, Type> settingsTypes = new();

    public JsonConnectionEx(string connectionString)
        : base(connectionString) { }

    public JsonConnectionEx(FileConnectionString fileConnectionString)
        : base(fileConnectionString) { }

    protected override Func<FileStatement, IDataSetWriter> CreateDataSetWriter => fileStatement => new JsonDataSetWriterEx(this, fileStatement, settingsTypes);
}
