using Server.Core;

namespace Server.Storage;

public interface IKvStorageEngine<Key, TValue>
{
    Task<Result> SaveDataAsync(Key key, TValue data);
    Task<Result<TValue>> LoadDataAsync(Key key);
    Task<Result<TValue>> DeleteDataAsync(Key key);
}