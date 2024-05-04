using System.Reflection;
using System.Text.Json.Serialization;
using Hangfire;
using Hangfire.Redis.StackExchange;
using LazyDan2.Filters;
using LazyDan2.Middleware;
using LazyDan2.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
var sqliteConnectionString = builder.Configuration.GetConnectionString("Sqlite");

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(x => {
    x.SingleLine = true;
    x.IncludeScopes = false;
});

builder.Services.AddHangfire(config =>
{
    config.UseRedisStorage(redisConnectionString, new RedisStorageOptions
    {
        Prefix = "LazyDan2",
        InvisibilityTimeout = TimeSpan.FromHours(12),
        SucceededListSize = 1000
    });
});

builder.Services.AddDbContext<GameContext>(options => options.UseSqlite(sqliteConnectionString));
builder.Services.AddTransient<GameService>();
builder.Services.AddTransient<StreamService>();
builder.Services.AddSingleton<PosterService>();

if (builder.Configuration.GetValue<bool>("UseHangfireServer"))
    builder.Services.AddHangfireServer();

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

builder.Services.AddMemoryCache();
builder.Services.AddControllers()
   .AddJsonOptions(options => options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);

builder.Services.AddHttpClient();
builder.Services.AddHealthChecks();
builder.Services.AddResponseCaching();

var app = builder.Build();

// SQLite

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<GameContext>();
    dbContext.Database.EnsureCreated();
}

// Middleware

app.UseMiddleware<ErrorLoggingMiddleware>();
app.UseMiddleware<DenyCloudflareMiddleware>();

// Hangfire

app.UseHangfireDashboard("/hangfire", new DashboardOptions {
Authorization = new [] { new HangfireAllowFilter() }
});

var recurringJobManager = app.Services.GetRequiredService<IRecurringJobManager>();
recurringJobManager.AddOrUpdate<GameService>("UpdateCfb", x => x.UpdateCfb(), Cron.Minutely);
recurringJobManager.AddOrUpdate<GameService>("UpdateMlb", x => x.UpdateMlb(), Cron.Minutely);
recurringJobManager.AddOrUpdate<GameService>("UpdateNba", x => x.UpdateNba(), Cron.Minutely);
recurringJobManager.AddOrUpdate<GameService>("UpdateNfl", x => x.UpdateNfl(), Cron.Minutely);
recurringJobManager.AddOrUpdate<GameService>("UpdateNhl", x => x.UpdateNhl(), Cron.Minutely);
recurringJobManager.AddOrUpdate<GameService>("UpdateEpg", x => x.UpdateEpg(), Cron.Daily);

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
