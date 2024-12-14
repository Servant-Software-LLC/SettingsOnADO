using System.Data;
using System.Globalization;
using System.Reflection;

namespace SettingsOnADO;

public class SettingsRepository : ISettingsRepository
{
    private readonly ISchemaManager schemaManager;
    private readonly IEncryptionProvider? encryptionProvider;

    public SettingsRepository(ISchemaManager schemaManager, IEncryptionProvider? encryptionProvider)
    {
        this.schemaManager = schemaManager ?? throw new ArgumentNullException(nameof(schemaManager));
        this.encryptionProvider = encryptionProvider;
    }

    public TSettingsEntity Get<TSettingsEntity>() where TSettingsEntity : class, new()
    {
        TSettingsEntity settings = new();

        var dataRow = schemaManager.GetRow(GetTableName<TSettingsEntity>());
        
        //Apply the settings from the database table to the settings object
        if (dataRow != null)
        {
            foreach (DataColumn dataColumn in dataRow.Table.Columns)
            {
                var property = typeof(TSettingsEntity).GetProperty(dataColumn.ColumnName);

                //Skip if the property does not exist in the entity
                if (property == null)
                    continue;

                if (dataRow[dataColumn] != DBNull.Value)
                {
                    object value = dataRow[dataColumn];
                    if (Attribute.IsDefined(property, typeof(EncryptedAttribute)) && encryptionProvider != null)
                    {
                        if (property.PropertyType != typeof(string) && !property.PropertyType.IsEnum)
                            throw new InvalidOperationException("Only string and enum properties can be encrypted.");

                        value = encryptionProvider.Decrypt((string)value);
                    }

                    Type targetType = property.PropertyType;
                    object convertedValue = ConvertValue(value, targetType);

                    property.SetValue(settings, convertedValue);
                }
            }
        }

        return settings;
    }

    public void Update<TSettingsEntity>(TSettingsEntity settings) where TSettingsEntity : class, new()
    {
        var tableName = GetTableName<TSettingsEntity>();
        var dataRow = schemaManager.GetRow(tableName);

        //Sort the properties of the entity (needed in either the case of creating the table or to determine
        //if we need to add/drop columns from the table)
        var entityProperties = typeof(TSettingsEntity).GetProperties().OrderBy(propInfo => propInfo.Name).ToList();

        List<InsertValue> insertColumns = new();
        if (dataRow == null)
        {
            //Create the table
            schemaManager.CreateTable(tableName, entityProperties);

            //Add the columns to the SQL INSERT command
            foreach (var property in entityProperties)
            {
                //Get the value of the property
                object? propertyValue = GetEncryptedPropertyValue(settings, property);

                if (propertyValue == null)
                    throw new ArgumentNullException(nameof(propertyValue));

                insertColumns.Add(new InsertValue(property.Name, propertyValue));
            }
        }
        else
        {
            //Sort the columns of the table
            var tableColumns = dataRow.Table.Columns.Cast<DataColumn>().OrderBy(col => col.ColumnName).ToList();

            //Walk both the properties and the columns of the table in order to determine if we need to add or drop columns
            int iProperty = 0;
            int iColumn = 0;

            while (iProperty < entityProperties.Count() && iColumn < tableColumns.Count())
            {
                var property = entityProperties[iProperty];
                var column = tableColumns[iColumn];

                var nameComparison = string.Compare(property.Name, column.ColumnName, StringComparison.OrdinalIgnoreCase);
                if (nameComparison == 0)
                {
                    //The column and the property have the same name

                    //Get the value of the property
                    var propertyValue = GetEncryptedPropertyValue(settings, property);

                    if (propertyValue == null)
                        throw new ArgumentNullException(nameof(propertyValue));

                    //Add the column for INSERT
                    insertColumns.Add(new InsertValue(property.Name, propertyValue));

                    iProperty++;
                    iColumn++;
                }
                else if (nameComparison < 0)
                {
                    //The property is not in the table.  Add the column.
                    schemaManager.AddColumn(tableName, property);

                    //Get the value of the property
                    object? propertyValue = GetEncryptedPropertyValue(settings, property);

                    if (propertyValue == null)
                        throw new ArgumentNullException(nameof(propertyValue));

                    insertColumns.Add(new InsertValue(property.Name, propertyValue));

                    iProperty++;

                }
                else
                {
                    //The column is not in the entity's properties. Drop the column.
                    schemaManager.DropColumn(typeof(TSettingsEntity).Name, column.ColumnName);
                    iColumn++;
                }
            }

            //Reached the end of one of the lists.  Add the remaining properties or columns from the other list.

            while (iProperty < entityProperties.Count())
            {
                var property = entityProperties[iProperty];
                schemaManager.AddColumn(tableName, property);

                //Get the value of the property
                object? propertyValue = GetEncryptedPropertyValue(settings, property);

                if (propertyValue == null)
                    throw new ArgumentNullException(nameof(propertyValue));

                insertColumns.Add(new InsertValue(property.Name, propertyValue));

                iProperty++;
            }

            while (iColumn < tableColumns.Count())
            {
                var column = tableColumns[iColumn];
                schemaManager.DropColumn(typeof(TSettingsEntity).Name, column.ColumnName);

                iColumn++;
            }

            //Delete the existing data
            schemaManager.DeleteTableData(tableName);
        }


        //Add the new data row to the table.
        schemaManager.InsertTableData(tableName, insertColumns);
    }

    /// <summary>
    /// Gets the value of a property and encrypts it if the property is marked with the EncryptedAttribute and the encryption provider is available.
    /// </summary>
    /// <param name="property">The property to get the value from.</param>
    /// <param name="settings">The settings object that contains the property.</param>
    /// <returns>The encrypted property value if the property is marked with the EncryptedAttribute and the encryption provider is available, otherwise the original property value.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the property is marked with the EncryptedAttribute but its type is not string.</exception>
    private object? GetEncryptedPropertyValue<TSettingsEntity>(TSettingsEntity settings, PropertyInfo? property) where TSettingsEntity : class, new()
    {
        if (property == null)
            throw new ArgumentNullException(nameof(property));

        var propertyValue = property.GetValue(settings);
        if (propertyValue == null)
            throw new ArgumentNullException(nameof(propertyValue));

        var propertyType = property.PropertyType;

        //Enum types are stored as strings.
        if (propertyType.IsEnum)
        {
            propertyValue = propertyValue.ToString();
            if (propertyValue == null)
                throw new ArgumentNullException(nameof(propertyValue));

            propertyType = typeof(string);
        }

        if (Attribute.IsDefined(property, typeof(EncryptedAttribute)) && encryptionProvider != null)
        {
            if (propertyType != typeof(string))
                throw new InvalidOperationException("Only string or enum properties can be encrypted.");

            propertyValue = encryptionProvider.Encrypt((string)propertyValue);
        }

        return propertyValue;
    }

    private object ConvertValue(object value, Type targetType)
    {
        if (value.GetType() != targetType)
        {
            //Enum types are stored as strings.  Convert the string to the enum type.
            if (targetType.IsEnum)
            {
                if (value is not string stringValue)
                    throw new Exception("Enum types must be stored as strings in the database.");

                return Enum.Parse(targetType, stringValue);
            }

            try
            {
                return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
            }
            catch (Exception ex)
            {
                // Log or handle the error as appropriate
                throw new InvalidOperationException($"Failed to convert {value.GetType().Name} to {targetType.Name}", ex);
            }
        }

        return value;
    }

    private string GetTableName<TSettingsEntity>() where TSettingsEntity : class =>
        typeof(TSettingsEntity).Name;
}
