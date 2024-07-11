using System.Collections.Concurrent;

namespace SettingsOnADO.Utils;

/// <summary>
/// Represents a thread-safe set of elements.
/// </summary>
/// <remarks>
/// This class uses a <see cref="System.Collections.Concurrent.ConcurrentDictionary{TKey, TValue}"/> internally to ensure thread safety.
/// </remarks>
/// <typeparam name="T">The type of elements in the set. The type must be non-nullable.</typeparam>
class ConcurrentSet<T>
    where T : notnull
{
    private readonly ConcurrentDictionary<T, byte> dictionary = new ConcurrentDictionary<T, byte>();

    public bool Add(T item) => dictionary.TryAdd(item, 0);

    public bool Remove(T item) => dictionary.TryRemove(item, out _);

    public bool Contains(T item) => dictionary.ContainsKey(item);

    public void Clear() => dictionary.Clear();

    public int Count => dictionary.Count;

    public IEnumerable<T> GetItems() => dictionary.Keys.ToList();
}