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
    x.TimestampFormat = "[MM-dd HH:mm:ss] ";
});

builder.Services.AddDbContext<GameContext>(options => options.UseSqlite(sqliteConnectionString));
builder.Services.AddScoped<GameService>();
builder.Services.AddScoped<StreamService>();
builder.Services.AddSingleton<PosterService>();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "LazyDan2", Version = "v1" });
});

var gameStreamProviders = Assembly.GetExecutingAssembly().GetTypes()
    .Where(t => typeof(IGameStreamProvider).IsAssignableFrom(t) && !t.IsInterface);

foreach (var type in gameStreamProviders)
{
    builder.Services.AddScoped(typeof(IGameStreamProvider), type);
}

// Coravel
builder.Services.AddScoped<DownloadGamesJob>();
builder.Services.AddScoped<QueueRecordingsJob>();
builder.Services.AddScoped<UpdateEpgJob>();
builder.Services.AddScoped<UpdateGamesJob>();
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
        .EveryFiveMinutes()
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
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "LazyDan2");
});

// Other HTTP stuff

app.UseResponseCaching();
app.UseStaticFiles();
app.UseRouting();

app.MapHealthChecks("/Health");
app.MapDefaultControllerRoute();
app.MapFallbackToFile("index.html"); ;

app.Run();
