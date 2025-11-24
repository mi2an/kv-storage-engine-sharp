using System.Net.Http.Headers;
using System.Text.Json;

const string Host = "http://localhost:5135"; // We don't bother creating a configuration file for now. It's simpler this way.

var data = File.ReadLinesAsync("test-data/put.txt");

using HttpClient client = CreateKvStoreEngineClient(Host);
File.Delete("error.log");
await foreach (var row in data)
{
    await ProcessRowRequest(row, client);
}
if (File.Exists("error.log"))
{
    Console.WriteLine("\nRESULT: Some errors were found during processing. See error.log for details. !!");
}
else
{
    Console.WriteLine("\nRESULT: All requests processed successfully with no errors.");
}



static HttpClient CreateKvStoreEngineClient(string host)
{
    HttpClient client = new()
    {
        BaseAddress = new Uri(host)
    };
    client.DefaultRequestHeaders.Accept.Clear();
    client.DefaultRequestHeaders.Accept.Add(
        new MediaTypeWithQualityHeaderValue("application/json"));
    return client;
}

static async Task ProcessRowRequest(string row, HttpClient client)
{
    if (string.IsNullOrWhiteSpace(row))
    {
        Console.WriteLine("Empty row.");
        return;
    }

    string[] spliced = row.Split(' ');
    if (spliced.Length != 3)
    {
        Console.WriteLine("Row does not contain exactly 3 parts.");
        return;
    }
    string action = spliced[0];
    string key = spliced[1];
    string value = spliced[2];

    Console.WriteLine($">>> Processing: {action} {key} {value}");
    switch (action)
    {
        case "PUT":
            string jsonValue = JsonSerializer.Serialize(value); //We have to make sure the result is interpreted as a JSON string.
            HttpContent putContent = new StringContent(jsonValue, System.Text.Encoding.UTF8, "application/json");
            HttpResponseMessage putResponse = await client.PutAsync($"/{key}", putContent);
            if (!putResponse.IsSuccessStatusCode)
            {
                File.AppendAllText("error.log", $"\nPUT {key} failed with status code: {putResponse.StatusCode}");
            }
            break;
        case "GET":
            HttpResponseMessage getResponse = await client.GetAsync($"/{key}");
            switch (getResponse.StatusCode)
            {
                case System.Net.HttpStatusCode.NotFound:
                    if (value != "NOT_FOUND")
                    {
                        File.AppendAllText("error.log", $"\nGET {key} error: Key not found (expected value {value}).");
                    }
                    break;
                default:
                    if (getResponse.IsSuccessStatusCode)
                    {
                        string body = await getResponse.Content.ReadAsStringAsync();
                        var result = JsonSerializer.Deserialize<string>(body);

                        if (result != value)
                        {
                            File.AppendAllText("error.log", $"\nGET {key} returned incorrect value: {result} (expected: {value})");
                        }
                        // What if value (from the file) is "NOT_FOUND" ? should we interpret it as "NOT_FOUND" string or as a missing key?
                        //Let's assume that the test data is well-formed and does not contain such contradictions.
                        break;
                    }
                    File.AppendAllText("error.log", $"\nGET {key} failed with status code: {getResponse.StatusCode}");
                    break;
            }
            break;
        default:
            break;
    }
}