using Core;

namespace Storage;

public interface IKvStoreEngine<Key, TValue>
{
    Task<Result> SaveDataAsync(Key key, TValue data);
    Task<Result<TValue>> LoadDataAsync(Key key);
    Task<Result<TValue>> DeleteDataAsync(Key key);
}