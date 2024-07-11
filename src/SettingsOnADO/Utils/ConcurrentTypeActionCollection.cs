using System.Collections.Concurrent;

namespace SettingsOnADO.Utils;

/// <summary>
/// Represents a thread-safe collection that maps types to sets of actions. Each type can have multiple actions associated with it.
/// </summary>
/// <remarks>
/// This class uses a <see cref="System.Collections.Concurrent.ConcurrentDictionary{TKey, TValue}"/> to ensure thread safety.
/// Actions are stored in a <see cref="ConcurrentSet{T}"/> to allow fast, thread-safe operations for adding, removing, and enumerating actions.
/// </remarks>
public class ConcurrentTypeActionCollection : IConcurrentTypeActionCollection
{
    private readonly ConcurrentDictionary<Type, ConcurrentSet<Delegate>> _dictionary = new();

    public void AddOrUpdate<TSettingsEntity>(Action<SettingsChangeEventArgs<TSettingsEntity>> action)
        where TSettingsEntity : class, new()
    {
        _dictionary.AddOrUpdate(
            typeof(TSettingsEntity),

            // If the key is not found, add a new ConcurrentSet with the action
            _ =>
            {
                ConcurrentSet<Delegate> newSet = new();
                newSet.Add(action);
                return newSet;
            },

            // If the key is found, add the action to the existing ConcurrentSet
            (_, set) =>
            {
                set.Add(action);
                return set;
            });
    }

    public bool Remove<TSettingsEntity>(Action<SettingsChangeEventArgs<TSettingsEntity>> action)
        where TSettingsEntity : class, new()
    {
        return _dictionary.TryGetValue(typeof(TSettingsEntity), out var set) && set.Remove(action);
    }

    public IEnumerable<Action<SettingsChangeEventArgs<TSettingsEntity>>> GetActions<TSettingsEntity>()
        where TSettingsEntity : class, new()
    {
        if (_dictionary.TryGetValue(typeof(TSettingsEntity), out var set))
        {
            return set.GetItems()
                .Cast<Action<SettingsChangeEventArgs<TSettingsEntity>>>()
                .ToList(); // Return a snapshot to avoid potential enumeration issues
        }
        return Enumerable.Empty<Action<SettingsChangeEventArgs<TSettingsEntity>>>();
    }
}

