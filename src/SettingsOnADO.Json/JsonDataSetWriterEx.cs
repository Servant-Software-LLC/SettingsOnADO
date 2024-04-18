using Data.Common.FileStatements;
using Data.Common.Interfaces;
using Data.Json.JsonIO;
using System.Data;
using System.Reflection;
using System.Text.Json;

namespace SettingsOnADO.Json;

public class JsonDataSetWriterEx : JsonDataSetWriter
{
    //This dictionary will hold the settings types for the settings entities.  Entries are added JIT to this dictionary
    //when the Get or Update methods are called in the JsonSettingsManager
    private readonly Dictionary<string, Type> settingsTypes;

    public JsonDataSetWriterEx(IFileConnection fileConnection, FileStatement fileStatement, Dictionary<string, Type> settingsTypes)
        : base(fileConnection, fileStatement)
    {
        this.settingsTypes = settingsTypes ?? throw new ArgumentNullException(nameof(settingsTypes));
    }

    protected override void WriteCommentValue(Utf8JsonWriter jsonWriter, DataColumn column)
    {
        var tableName = column.Table!.TableName;

        if (!settingsTypes.TryGetValue(tableName, out var settingsType))
            throw new Exception($"No {nameof(settingsTypes)} for data type {tableName}");

        var property = settingsType.GetProperty(column.ColumnName);
        if (property == null)
            //The setting property does not exist in the settings entity.  This occurs when a settings property has been deprecated
            return;

        //See if the settings property has the JsonDescriptionAttribute
        var jsonDescriptionAttribute = property.GetCustomAttribute<JsonDescriptionAttribute>();
        if (jsonDescriptionAttribute != null)
        {
            jsonWriter.WriteCommentValue(jsonDescriptionAttribute.Description);
        }
    }
}
