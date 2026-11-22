
using System.Reflection;
using KIRA.API.Configurations;

var builder = WebApplication.CreateBuilder(args);

// Register MVC Controllers
builder.Services.AddControllers();

// Register API Explorer
builder.Services.AddEndpointsApiExplorer();

// Configure Swagger
builder.Services.AddSwaggerGen(options =>
{
    // Get the generated XML documentation file name
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";

    // Get the full path of the XML documentation file
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

    // Include XML comments in Swagger documentation
    options.IncludeXmlComments(xmlPath);
});

// Register Infrastructure Services
builder.Services.AddInfrastructureServices(builder.Configuration);

// Register Application Services
builder.Services.AddApplicationServices();

// Register Authentication Rate Limiting
builder.Services.AddAuthRateLimiting();

var app = builder.Build();

// Initialize Database
using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider
        .GetRequiredService<IInializar>();

    var result = await initializer.Inializ();

    if (!result.IsSuccess)
    {
        throw new Exception(
            "Database initialization failed. Check the initialization result.");
    }
}

// Configure HTTP Request Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();

app.UseRateLimiter();

app.UseAuthentication();

app.UseAuthorization();

// Configure Hangfire Dashboard
if (app.Environment.IsDevelopment())
{
    app.UseHangfireDashboard("/hangfire");
}

// Map Controllers
app.MapControllers();

app.Run();
