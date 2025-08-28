using A3sist.API.Services;
using A3sist.API.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// A3sist Core Services
builder.Services.AddSingleton<IModelManagementService, ModelManagementService>();
builder.Services.AddSingleton<IChatService, ChatService>();
builder.Services.AddSingleton<ICodeAnalysisService, CodeAnalysisService>();
builder.Services.AddSingleton<IRefactoringService, RefactoringService>();
builder.Services.AddSingleton<IRAGEngineService, RAGEngineService>();
builder.Services.AddSingleton<IMCPClientService, MCPClientService>();
builder.Services.AddSingleton<IAutoCompleteService, AutoCompleteService>();
builder.Services.AddSingleton<IAgentModeService, AgentModeService>();

// HTTP Client Factory for efficient resource management
builder.Services.AddHttpClient("ModelClient", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "A3sist-API/1.0");
});

builder.Services.AddHttpClient("MCPClient", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "A3sist-MCP/1.0");
});

builder.Services.AddHttpClient("RAGClient", client =>
{
    client.Timeout = TimeSpan.FromSeconds(60);
    client.DefaultRequestHeaders.Add("User-Agent", "A3sist-RAG/1.0");
});

// CORS for VS Extension communication
builder.Services.AddCors(options =>
{
    options.AddPolicy("VSExtension", policy =>
    {
        policy.WithOrigins("http://localhost", "https://localhost")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Windows Service support
builder.Services.AddWindowsService();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("VSExtension");
app.UseRouting();

app.MapControllers();
app.MapHub<A3sistHub>("/a3sistHub");

// Health check endpoint
app.MapGet("/api/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

// Default port for A3sist API
app.Urls.Add("http://localhost:8341");

Console.WriteLine("A3sist API Server starting on http://localhost:8341");
Console.WriteLine("Swagger UI available at http://localhost:8341/swagger");

app.Run();