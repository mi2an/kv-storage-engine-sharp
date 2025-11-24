namespace Server.Storage;

public class MemTable<TKey, TValue>(IComparer<TKey>? comparer = default) where TKey : notnull
{
    private readonly SemaphoreSlim Semaphore = new(1, 1);
    public TValue this[TKey key]
    {
        set
        {
            Semaphore.Wait();
            Table[key] = value;
            Semaphore.Release();
        }
    }

    public int Count
    {
        get
        {
            Semaphore.Wait();
            var res = Table.Count;
            Semaphore.Release();
            return res;
        }
    }

    public bool TryGetValue(TKey key, out TValue? value)
    {
        Semaphore.Wait();
        var res = Table.TryGetValue(key, out value);
        Semaphore.Release();
        return res;
    }

    public async IAsyncEnumerable<KeyValuePair<TKey, TValue>> FlushAsync()
    {
        Semaphore.Wait();
        foreach (var kvp in Table)
        {
            yield return kvp;
        }
        Table.Clear();
        Semaphore.Release();
    }

    private SortedDictionary<TKey, TValue> Table { get; } = new SortedDictionary<TKey, TValue>(comparer);
}