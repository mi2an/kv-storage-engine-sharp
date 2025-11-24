namespace Server.Storage;

public class MemTable<TKey, TValue>(IComparer<TKey>? comparer = default) where TKey : notnull
{
    public TValue this[TKey key]
    {
        set {
            lock (Table) { 
                Table[key] = value; 
            }
        }
    }

    public int Count => Table.Count;

    public bool TryGetValue(TKey key, out TValue? value)
    {
        return Table.TryGetValue(key, out value);
    }

    public async IAsyncEnumerable<KeyValuePair<TKey, TValue>> FlushAsync()
    {
        lock(Table)
        {
            foreach (var kvp in Table)
            {
                yield return kvp;
            }
            Table.Clear();
        }
    }

    private SortedDictionary<TKey, TValue> Table { get; } = new SortedDictionary<TKey, TValue>(comparer);
}