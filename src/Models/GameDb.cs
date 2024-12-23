using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public class Game
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public string League { get; set; }
    public string HomeTeam { get; set; }
    public string AwayTeam { get; set; }
    public DateTime GameTime { get; set; }
    public string State { get; set;}
    public bool DownloadSelected { get; set; }
    public bool DownloadStarted { get; set; }
    public bool DownloadCompleted { get; set; }
    public string ShortHomeTeam => League == LazyDan2.Types.League.Cfb
        ? HomeTeam?.Replace(' ', '-').ToLower()
        : HomeTeam?.Split(' ').Last().ToLower();
    public string ShortAwayTeam => League == LazyDan2.Types.League.Cfb
        ? AwayTeam?.Replace(' ', '-').ToLower()
        : AwayTeam?.Split(' ').Last().ToLower();
}

public class GameContext : DbContext
{
    public GameContext(DbContextOptions<GameContext> options) : base(options)
    {
    }

    public DbSet<Game> Games { get; set; }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
    }
}

public class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter()
        : base(
            static datetime => datetime.ToUniversalTime(),
            static datetime => DateTime.SpecifyKind(datetime, DateTimeKind.Utc)
            )
    {
    }
}