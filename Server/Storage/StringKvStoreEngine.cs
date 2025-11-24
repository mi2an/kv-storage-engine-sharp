using Core;
using System.Collections.Concurrent;

namespace Storage;

public class StringKvStoreEngine: IKvStoreEngine<string, string>
{
    //Do not fancy it up, just use a simple concurrent dictionary for storage.
    private ConcurrentDictionary<string, string> _store = new ConcurrentDictionary<string, string>();


    public async Task<Result> SaveDataAsync(string key, string data) {
        var validationResult = ValidateKey(key);
        if (!validationResult.IsSuccess) {
            return validationResult;
        }
        validationResult = ValidateValue(data);
        if (!validationResult.IsSuccess) {
            return validationResult;
        }
        
        try {
            _store[key] = data;
            return Result.Success();
        } catch (Exception ex) {
            return Result.Failure(Error.InternalError($"Failed to save data for key '{key}': {ex.Message}"));
        }
    }
    public async Task<Result<string>> LoadDataAsync(string key) {
        if (!_store.TryGetValue(key, out var value)) {
            return Result<string>.Failure(Error.NotFound($"Key '{key}' not found."));
        }
        return Result<string>.Success(value);
    }

    public async Task<Result<string>> DeleteDataAsync(string key) {
        throw new NotImplementedException();
    }


    protected Result ValidateKey(string key) {
        if (string.IsNullOrWhiteSpace(key)) {
            return Result.Failure(Error.InvalidInput("Key cannot be null or whitespace."));
        }
        if (!key.All(char.IsAscii)) {
            return Result.Failure(Error.InvalidInput("Key must contain only ASCII characters."));
        }
        if (key.Any(char.IsUpper)) {
            return Result.Failure(Error.InvalidInput("Key cannot contain uppercase letters."));
        }
        //for now, we don't have any other restrictions:
        // No "trim" needed,
        // No length restrictions,
        // No special character restrictions.
        return Result.Success();
    }

    protected Result ValidateValue(string value) {
        if (value is null) {
            return Result.Success();
        }
        if (!value.All(char.IsAscii)) {
            return Result.Failure(Error.InvalidInput("Value must contain only ASCII characters."));
        }
        return Result.Success();
    }
}