using Storage;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddSingleton<IKvStoreEngine<string, string>, StringKvStoreEngine>();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/", () =>
{
    return "Welcome, Adventurer!";
});



app.MapGet("/{key}", async (string key, IKvStoreEngine<string, string> store) =>
{
    var result = await store.LoadDataAsync(key);
    return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
});

app.MapPut("/{key}", async (string key, [FromBody] string value, IKvStoreEngine<string, string> store) =>
{
    var result = await store.SaveDataAsync(key, value);
    return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.Error);
});




app.Run();
