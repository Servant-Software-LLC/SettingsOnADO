using System.Data.Common;
using System.Reflection;

namespace SettingsOnADO.Utils;

internal static class AdoNetProviderHelper
{
    //NOTE: Since .NET Stanadard 2.0 does not have DbProviderFactories, we need to create a helper method to get the
    //DbDataAdapter.  Furthermore, this is compounded with the fact that currently the ServantSoftware FileBased DataProviders
    //do not implement a DbProviderFactory.
    public static DbDataAdapter CreateDataAdapter(DbConnection connection)
    {
        // Start with the actual type of the connection
        Type currentType = connection.GetType();

        Assembly previousAssembly = null;
        while (currentType != null && currentType != typeof(DbConnection))
        {
            // Get the assembly of the current type
            Assembly assembly = currentType.Assembly;

            // If the assembly is the same as the previous one, there is no need to search again
            if (assembly != previousAssembly)
            {
                // Find a concrete class that derives from DbDataAdapter in the current assembly
                Type adapterType = assembly.GetTypes().FirstOrDefault(
                    t => !t.IsAbstract &&
                         t.IsPublic &&
                         typeof(DbDataAdapter).IsAssignableFrom(t));

                // If a suitable DbDataAdapter is found, create and return it
                if (adapterType != null)
                {
                    var adapter = (DbDataAdapter)Activator.CreateInstance(adapterType);
                    return adapter;
                }

                previousAssembly = assembly;
            }

            // Move to the base type if no suitable adapter is found in the current assembly
            currentType = currentType.BaseType;
        }

        // If the loop completes without finding an adapter, throw an exception
        throw new InvalidOperationException("No DbDataAdapter found for the provided connection.");
    }

}
