
using AskTheDocs.Services;

var builder = WebApplication.CreateBuilder(args);

// Register services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Depende
builder.Services.AddSingleton<PdfTextExtractor>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Health check
app.MapGet("/api/health", () => Results.Ok(new
{
    status = "Healthy",
    application = "Ask the Docs"
}));

// Extract text from the sample PDF
app.MapGet("/api/documents/sample/text", (
    PdfTextExtractor objExtractor,
    IWebHostEnvironment environment) =>
{
    var strFilePath = Path.Combine(
        environment.ContentRootPath,
        "DocumentsToRead",
        "pip2-form-and-information-booklet.pdf");

    if (!File.Exists(strFilePath))
    {
        return Results.NotFound("Sample PDF not found.");
    }

    var text = objExtractor.ExtractText(strFilePath);

    return Results.Ok(new { text });
});

app.Run();
