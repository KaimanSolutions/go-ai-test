using FormBuilder.Backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace FormBuilder.Backend.Data;

public class FormBuilderDbContext : DbContext
{
    public FormBuilderDbContext(DbContextOptions<FormBuilderDbContext> options) : base(options)
    {
    }

    public DbSet<FormSchema> FormSchemas { get; set; }
    public DbSet<FormStep> FormSteps { get; set; }
    public DbSet<FormField> FormFields { get; set; }
    public DbSet<ValidationRule> ValidationRules { get; set; }
    public DbSet<FieldCondition> FieldConditions { get; set; }
    public DbSet<HelpArticle> HelpArticles { get; set; }
    public DbSet<BrandingSettings> BrandingSettings { get; set; }
    public DbSet<ProfileSettings> ProfileSettings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure FormSchema
        modelBuilder.Entity<FormSchema>()
            .HasKey(f => f.Id);

        modelBuilder.Entity<FormSchema>()
            .Property(f => f.Id)
            .ValueGeneratedNever();

        modelBuilder.Entity<FormSchema>()
            .HasMany(f => f.Steps)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        // Configure FormStep
        modelBuilder.Entity<FormStep>()
            .HasMany(s => s.Fields)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        // Configure FormField
        modelBuilder.Entity<FormField>()
            .HasMany(f => f.Validators)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<FormField>()
            .HasMany(f => f.Conditions)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<FormField>()
            .Ignore(f => f.DefaultValue);

        modelBuilder.Entity<FieldCondition>()
            .Ignore(c => c.Value);

        modelBuilder.Entity<ValidationRule>()
            .Ignore(v => v.Value);

        var stringListComparer = new ValueComparer<List<string>>(
            (c1, c2) => c1 == null && c2 == null || c1 != null && c2 != null && c1.SequenceEqual(c2),
            c => c == null ? 0 : c.Aggregate(0, (a, v) => HashCode.Combine(a, v == null ? 0 : v.GetHashCode())),
            c => c == null ? new List<string>() : c.ToList()
        );

        modelBuilder.Entity<FormField>()
            .Property(f => f.Options)
            .HasConversion(
                v => string.Join(";", v ?? new List<string>()),
                v => string.IsNullOrEmpty(v) ? new List<string>() : v.Split(";", StringSplitOptions.RemoveEmptyEntries).ToList()
            )
            .Metadata.SetValueComparer(stringListComparer);

        // Configure HelpArticle
        modelBuilder.Entity<HelpArticle>()
            .HasKey(a => a.Id)
            .IsClustered();

        // Configure BrandingSettings
        modelBuilder.Entity<BrandingSettings>()
            .HasKey(b => b.Id)
            .IsClustered();

        // Configure ProfileSettings
        modelBuilder.Entity<ProfileSettings>()
            .HasKey(p => p.Id);
    }
}
