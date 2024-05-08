using System.Reflection;
using System.Text.Json.Serialization;
using Coravel;
using LazyDan2.Jobs;
using LazyDan2.Middleware;
using LazyDan2.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
var environmentName = builder.Environment.EnvironmentName;

var sqliteConnectionString = builder.Configuration.GetConnectionString("Sqlite");

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(x => {
    x.SingleLine = true;
    x.IncludeScopes = false;
});

builder.Services.AddDbContext<GameContext>(options => options.UseSqlite(sqliteConnectionString), ServiceLifetime.Transient);
builder.Services.AddTransient<GameService>();
builder.Services.AddTransient<StreamService>();
builder.Services.AddSingleton<PosterService>();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "lazydan", Version = "v1" });
});

var gameStreamProviders = Assembly.GetExecutingAssembly().GetTypes()
    .Where(t => typeof(IGameStreamProvider).IsAssignableFrom(t) && !t.IsInterface);

foreach (var type in gameStreamProviders)
{
    builder.Services.AddTransient(typeof(IGameStreamProvider), type);
}

// Coravel
builder.Services.AddTransient<DownloadGamesJob>();
builder.Services.AddTransient<QueueRecordingsJob>();
builder.Services.AddTransient<UpdateEpgJob>();
builder.Services.AddTransient<UpdateGamesJob>();
builder.Services.AddScheduler();
builder.Services.AddQueue();

builder.Services.AddMemoryCache();
builder.Services.AddControllers()
   .AddJsonOptions(options => options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);

builder.Services.AddHttpClient();
builder.Services.AddHealthChecks();
builder.Services.AddResponseCaching();

var app = builder.Build();

var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Starting LazyDan2 in {EnvironmentName} environment", environmentName);

// SQLite

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<GameContext>();
    dbContext.Database.EnsureCreated();
}

// Coravel
app.Services.UseScheduler(scheduler => {
    scheduler
        .Schedule<UpdateGamesJob>()
        .EveryMinute()
        .PreventOverlapping(nameof(UpdateGamesJob));

    scheduler
        .Schedule<QueueRecordingsJob>()
        .EveryMinute()
        .PreventOverlapping(nameof(QueueRecordingsJob));

    scheduler
        .Schedule<UpdateEpgJob>()
        .Daily();
}).OnError(x => logger.LogError(x, x.Message));

// Middleware

app.UseMiddleware<ErrorLoggingMiddleware>();
app.UseMiddleware<DenyCloudflareMiddleware>();

// Swagger

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "LazyDan V1");
});

// Other HTTP stuff

app.UseResponseCaching();
app.UseStaticFiles();
app.UseRouting();

app.MapHealthChecks("/Health");
app.MapDefaultControllerRoute();
app.MapFallbackToFile("index.html"); ;

app.Run();
