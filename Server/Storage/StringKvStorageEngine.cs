using System.Text;
using System.Text.Json;
using Server.Core;

namespace Server.Storage;

public class StringKvStorageEngine(StringKvStorageEngine.StringKvStorageEngineConfiguration Configuration) : IKvStorageEngine<string, string>
{
    private MemTable<string, string> Storage { get; } = new(StringComparer.Ordinal);

    public async Task<Result> SaveDataAsync(string key, string data)
    {
        var validationResult = ValidateKey(key);
        if (!validationResult.IsSuccess)
        {
            return validationResult;
        }
        validationResult = ValidateValue(data);
        if (!validationResult.IsSuccess)
        {
            return validationResult;
        }

        try
        {
            Storage[key] = data;
        }
        catch (Exception ex)
        {
            return Result.Failure(Error.InternalError($"Failed to save data for key '{key}': {ex.Message}"));
        }

        var flushResult = await FlushData();
        if (!flushResult.IsSuccess)
        {
            return flushResult;
        }

        return Result.Success();
    }

    private async Task<Result> FlushData()
    {
        if (Storage.Count < Configuration.MemtableCapacity)
        {
            return Result.Success();
        }
        var asyncRows = File.ReadLinesAsync(Configuration.ManifestFile, Encoding.UTF8);
        bool isFirstLine = true;
        string pattern = "";
        string mostRecentRow = "";
        await foreach (var line in asyncRows)
        {
            if (isFirstLine)
            {
                isFirstLine = false;
                pattern = line;
                continue;
            }
            mostRecentRow = line;
        }

        string nextSStable;
        if (string.IsNullOrWhiteSpace(mostRecentRow))
        {
            var spliced = pattern.Split('.', StringSplitOptions.RemoveEmptyEntries);
            nextSStable = $"{spliced[0]}1.{spliced[1]}";
        }
        else
        {
            var splicedPattern = pattern.Split('.', StringSplitOptions.RemoveEmptyEntries);
            var spliced = mostRecentRow.Split('.', StringSplitOptions.RemoveEmptyEntries);
            var numberPart = int.Parse(spliced[0].Replace(splicedPattern[0], "")) + 1;
            nextSStable = $"{splicedPattern[0]}{numberPart}.{splicedPattern[1]}";
        }

        try
        {
            using var fs = new FileStream(
                Path.Combine(Path.GetDirectoryName(Configuration.ManifestFile)!, nextSStable)
                , FileMode.Create, FileAccess.Write, FileShare.None
            );

            await JsonSerializer.SerializeAsync(fs, Storage.FlushAsync().Select(kvp => new { kvp.Key, kvp.Value }));

            await File.AppendAllLinesAsync(Configuration.ManifestFile, [nextSStable]);
        }
        catch (Exception ex)
        {
            return Result.Failure(Error.InternalError($"Failed to flush data to SSTable '{nextSStable}': {ex.Message}"));
        }
        return Result.Success();
    }

    public async Task<Result<string>> LoadDataAsync(string key)
    {
        if (Storage.TryGetValue(key, out var value))
        {
            return Result<string>.Success(value!);
        }

        var rows = (await File.ReadAllLinesAsync(Configuration.ManifestFile, Encoding.UTF8)).Where((_, i) => i > 0);
        var stackFiles = new Stack<string>(rows);
        var dir = Path.GetDirectoryName(Configuration.ManifestFile)!;
        while (stackFiles.TryPop(out var lastFile))
        {
            var path = Path.Combine(dir, lastFile);
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            await foreach (var kvp in JsonSerializer.DeserializeAsyncEnumerable<dynamic>(fs))
            {
                var sstK = kvp!.GetProperty("Key").ToString();
                if (sstK == key)
                {
                    return Result<string>.Success(kvp.GetProperty("Value").ToString());
                }
            }
        }

        return Result<string>.Failure(Error.NotFound($"Key '{key}' not found."));
    }

    public async Task<Result<string>> DeleteDataAsync(string key)
    {
        throw new NotImplementedException();
    }




    protected Result ValidateKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return Result.Failure(Error.InvalidInput("Key cannot be null or whitespace."));
        }
        if (!key.All(char.IsAscii))
        {
            return Result.Failure(Error.InvalidInput("Key must contain only ASCII characters."));
        }
        if (key.Any(char.IsUpper))
        {
            return Result.Failure(Error.InvalidInput("Key cannot contain uppercase letters."));
        }
        //for now, we don't have any other restrictions:
        // No "trim" needed,
        // No length restrictions,
        // No special character restrictions.
        return Result.Success();
    }

    protected Result ValidateValue(string value)
    {
        if (value is null)
        {
            return Result.Success();
        }
        if (!value.All(char.IsAscii))
        {
            return Result.Failure(Error.InvalidInput("Value must contain only ASCII characters."));
        }
        return Result.Success();
    }

    public record StringKvStorageEngineConfiguration(string ManifestFile, int MemtableCapacity);
}