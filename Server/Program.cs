using Microsoft.AspNetCore.Mvc;
using Server.Storage;
using static Server.Storage.StringKvStorageEngine;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddSingleton<IKvStorageEngine<string, string>, StringKvStorageEngine>(o =>
{
    const int MAX_ELEMENTS = 2000; //Instructed in the blog: week-2
    return new StringKvStorageEngine(new StringKvStorageEngineConfiguration("Data/Manifest", MAX_ELEMENTS));
});
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



app.MapGet("/{key}", async (string key, IKvStorageEngine<string, string> store) =>
{
    var result = await store.LoadDataAsync(key);
    return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
});

app.MapPut("/{key}", async (string key, [FromBody] string value, IKvStorageEngine<string, string> store) =>
{
    var result = await store.SaveDataAsync(key, value);
    return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.Error);
});




app.Run();
