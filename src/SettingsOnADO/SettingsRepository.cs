using System.Data;
using System.Globalization;

namespace SettingsOnADO;

public class SettingsRepository : ISettingsRepository
{
    private readonly ISchemaManager schemaManager;

    public SettingsRepository(ISchemaManager schemaManager)
    {
        this.schemaManager = schemaManager ?? throw new ArgumentNullException(nameof(schemaManager));
    }

    public TSettingsEntity Get<TSettingsEntity>() where TSettingsEntity : class, new()
    {
        TSettingsEntity settings = new();

        var dataRow = schemaManager.GetRow(typeof(TSettingsEntity).Name);
        
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
                    Type targetType = property.PropertyType;
                    object convertedValue = ConvertValue(value, targetType);

                    property.SetValue(settings, convertedValue);
                }
            }
        }

        return settings;
    }

    private object ConvertValue(object value, Type targetType)
    {
        if (value.GetType() != targetType)
        {
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

    public void Update<TSettingsEntity>(TSettingsEntity settings) where TSettingsEntity : class, new()
    {
        var tableName = typeof(TSettingsEntity).Name;
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
                var propertyValue = property.GetValue(settings);

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
                    var propertyValue = property.GetValue(settings);

                    object convertedValue = ConvertValue(propertyValue, column.DataType);

                    //Get the value of the column
                    var columnValue = dataRow[column];

                    //Check if the value of the property is different from the value of the column
                    if (!object.Equals(propertyValue, columnValue))
                    {
                        insertColumns.Add(new InsertValue(property.Name, propertyValue));
                    }

                    iProperty++;
                    iColumn++;
                }
                else if (nameComparison < 0)
                {
                    //The property is not in the table.  Add the column.
                    schemaManager.AddColumn(tableName, property);

                    //Get the value of the property
                    var propertyValue = property.GetValue(settings);

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
                var propertyValue = property.GetValue(settings);

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

}
