
using AskTheDocs.Configuration;
using AskTheDocs.Models;
using AskTheDocs.Services;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using System.Reflection.Metadata;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();

// Register services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

#region Dependencies
// Dependendcies

builder.Services.AddSingleton<PdfTextExtractor>();
builder.Services.AddSingleton<TextChunker>();

builder.Services
    .AddOptions<OllamaOptions>()
    .Bind(builder.Configuration.GetSection(OllamaOptions.SectionName))
    .Validate(options =>
        Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttp ||
         uri.Scheme == Uri.UriSchemeHttps),
        "Ollama BaseUrl must be a valid HTTP or HTTPS URL.")
    .Validate(options =>
        !string.IsNullOrWhiteSpace(options.EmbeddingModel),
        "Ollama EmbeddingModel must be configured.")
    .ValidateOnStart();

builder.Services.AddHttpClient<EmbeddingService>((serviceProvider, client) =>
{
    var options = serviceProvider
        .GetRequiredService<
            Microsoft.Extensions.Options.IOptions<OllamaOptions>>()
        .Value;

    client.BaseAddress = new Uri(options.BaseUrl);
    client.Timeout = TimeSpan.FromMinutes(5);
});

builder.Services.AddHttpClient<QueryExpansionService>((serviceProvider, client) =>
{
    var options = serviceProvider
        .GetRequiredService<
            Microsoft.Extensions.Options.IOptions<OllamaOptions>>()
        .Value;

    client.BaseAddress = new Uri(options.BaseUrl);
    client.Timeout = TimeSpan.FromMinutes(5);
});

builder.Services.AddSingleton<DocumentIndexService>();

builder.Services.AddHttpClient<AnswerGenerationService>(
    (serviceProvider, client) =>
    {
        var options = serviceProvider
            .GetRequiredService<
                Microsoft.Extensions.Options.IOptions<OllamaOptions>>()
            .Value;

        client.BaseAddress = new Uri(options.BaseUrl);
        client.Timeout = TimeSpan.FromMinutes(5);
    });

builder.Services.AddSingleton<AddressExtractor>();
builder.Services.AddSingleton<CosineSimilarityService>();

builder.Services.AddSingleton(_ =>
    new QdrantClient(
        host: builder.Configuration["Qdrant:Host"]!,
        port: builder.Configuration.GetValue<int>("Qdrant:Port")));

builder.Services.AddSingleton<QdrantDocumentStore>();

#endregion

builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();

var qdrantClient = app.Services.GetRequiredService<QdrantClient>();

var collectionName = app.Configuration["Qdrant:CollectionName"]!;
var vectorSize = app.Configuration.GetValue<int>("Qdrant:VectorSize");

if (!await qdrantClient.CollectionExistsAsync(collectionName))
{
    await qdrantClient.CreateCollectionAsync(
        collectionName,
        new VectorParams
        {
            Size = (ulong)vectorSize,
            Distance = Distance.Cosine
        });
}

// Configure the HTTP request pipeline
app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        var exceptionFeature =
            context.Features.Get<
                Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();

        var exception = exceptionFeature?.Error;

        var statusCode = exception switch
        {
            AskTheDocs.Exceptions.OllamaUnavailableException
                => StatusCodes.Status503ServiceUnavailable,

            TimeoutException
                => StatusCodes.Status504GatewayTimeout,

            _ => StatusCodes.Status500InternalServerError
        };

        context.Response.StatusCode = statusCode;

        await Results.Problem(
            statusCode: statusCode,
            title: statusCode switch
            {
                503 => "An AI service is currently unavailable.",
                504 => "The AI service took too long to respond.",
                _ => "An unexpected error occurred."
            },
            instance: context.Request.Path
        ).ExecuteAsync(context);
    });
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.Run();
