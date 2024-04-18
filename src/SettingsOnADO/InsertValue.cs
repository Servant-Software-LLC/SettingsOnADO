namespace SettingsOnADO;

public class InsertValue
{
    public InsertValue(string columnName, object value)
    {
        ColumnName = columnName;
        Value = value;
    }

    public string ColumnName { get; }
    public object Value { get; }
}
