using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Revalidate.Converters.Db;
using Revalidate.Entities;
using System.Text.Json;
using TmEssentials;

namespace Revalidate;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<ValidationRequestEntity> ValidationRequests { get; set; }
    public DbSet<ValidationResultEntity> ValidationResults { get; set; }
    public DbSet<FileEntity> Files { get; set; }
    public DbSet<GhostInputEntity> GhostInputs { get; set; }
    public DbSet<GhostCheckpointEntity> GhostCheckpoints { get; set; }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder
            .Properties<TimeInt32>()
            .HaveConversion<DbTimeInt32Converter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var dictConverter = new ValueConverter<Dictionary<string, string[]>, string>(
            v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
            v => JsonSerializer.Deserialize<Dictionary<string, string[]>>(v, JsonSerializerOptions.Default)!
        );

        modelBuilder.Entity<ValidationRequestEntity>()
            .Property(x => x.Warnings)
            .HasConversion(dictConverter)
            .HasColumnType("JSON");

        modelBuilder.Entity<ValidationResultEntity>()
            .Property(x => x.Problems)
            .HasConversion(dictConverter)
            .HasColumnType("JSON");
    }
}