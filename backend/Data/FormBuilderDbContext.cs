using FormBuilder.Backend;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Text.Json;

namespace FormBuilder.Backend.Data;

public sealed class FormBuilderDbContext : DbContext
{
    public FormBuilderDbContext(DbContextOptions<FormBuilderDbContext> options) : base(options) { }

    public DbSet<FormSchema>       FormSchemas        { get; set; }
    public DbSet<FormStep>         FormSteps          { get; set; }
    public DbSet<FormField>        FormFields         { get; set; }
    public DbSet<ValidationRule>   ValidationRules    { get; set; }
    public DbSet<FieldCondition>   FieldConditions    { get; set; }
    public DbSet<HelpArticle>      HelpArticles       { get; set; }
    public DbSet<BrandingSettings> BrandingSettings   { get; set; }
    public DbSet<UserAccount>      UserAccounts       { get; set; }
    public DbSet<Company>          Companies          { get; set; }
    public DbSet<Address>          Addresses          { get; set; }
    public DbSet<TradingName>      TradingNames       { get; set; }
    public DbSet<BankDetails>      BankDetails        { get; set; }
    public DbSet<IntegrationSetting>  IntegrationSettings  { get; set; }
    public DbSet<ApiRequestLog>       ApiRequestLogs       { get; set; }
    public DbSet<Workflow>            Workflows            { get; set; }
    public DbSet<WorkflowStage>       WorkflowStages       { get; set; }
    public DbSet<WorkflowTask>        WorkflowTasks        { get; set; }
    public DbSet<WorkflowTransition>  WorkflowTransitions  { get; set; }
    public DbSet<Application>         Applications         { get; set; }
    public DbSet<BusinessRule>        BusinessRules        { get; set; }
    public DbSet<RuleCondition>       RuleConditions       { get; set; }
    public DbSet<RuleOutcome>         RuleOutcomes         { get; set; }
    public DbSet<ChecklistItem>             ChecklistItems             { get; set; }
    public DbSet<ChecklistCondition>        ChecklistConditions        { get; set; }
    public DbSet<ApplicationChecklistItem>   ApplicationChecklistItems   { get; set; }
    public DbSet<ApplicationChecklistComment> ApplicationChecklistComments { get; set; }
    public DbSet<Template>                   Templates                   { get; set; }
    public DbSet<ApplicationNote>            ApplicationNotes            { get; set; }
    public DbSet<LoginEvent>                 LoginEvents                 { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<FormSchema>()
            .HasKey(f => f.Id);

        modelBuilder.Entity<FormSchema>()
            .Property(f => f.Id)
            .ValueGeneratedNever();

        modelBuilder.Entity<FormSchema>()
            .HasMany(f => f.Steps).WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<FormSchema>()
            .HasOne(f => f.Workflow)
            .WithMany()
            .HasForeignKey(f => f.WorkflowId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<FormStep>()
            .HasMany(s => s.Fields).WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<FormField>()
            .HasMany(f => f.Validators).WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<FormField>()
            .HasMany(f => f.Conditions).WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<FormField>().Ignore(f => f.DefaultValue);

        var stringListComparer = new ValueComparer<List<string>>(
            (c1, c2) => c1 == null && c2 == null || c1 != null && c2 != null && c1.SequenceEqual(c2),
            c => c == null ? 0 : c.Aggregate(0, (a, v) => HashCode.Combine(a, v == null ? 0 : v.GetHashCode())),
            c => c == null ? new List<string>() : c.ToList());

        modelBuilder.Entity<FormField>()
            .Property(f => f.Options)
            .HasConversion(
                v => string.Join(";", v ?? new List<string>()),
                v => string.IsNullOrEmpty(v) ? new List<string>() : v.Split(";", StringSplitOptions.RemoveEmptyEntries).ToList())
            .Metadata.SetValueComparer(stringListComparer);

        var subFieldListComparer = new ValueComparer<List<SubFieldDefinition>>(
            (c1, c2) => c1 == null && c2 == null || (c1 != null && c2 != null && JsonSerializer.Serialize(c1) == JsonSerializer.Serialize(c2)),
            c => c == null ? 0 : JsonSerializer.Serialize(c).GetHashCode(),
            c => c == null ? new List<SubFieldDefinition>() : JsonSerializer.Deserialize<List<SubFieldDefinition>>(JsonSerializer.Serialize(c), (JsonSerializerOptions?)null) ?? new List<SubFieldDefinition>());

        modelBuilder.Entity<FormField>()
            .Property(f => f.SubFields)
            .HasConversion(
                v => JsonSerializer.Serialize(v ?? new List<SubFieldDefinition>(), (JsonSerializerOptions?)null),
                v => string.IsNullOrEmpty(v) ? new List<SubFieldDefinition>() : JsonSerializer.Deserialize<List<SubFieldDefinition>>(v, (JsonSerializerOptions?)null) ?? new List<SubFieldDefinition>())
            .Metadata.SetValueComparer(subFieldListComparer);

        var stepCondListComparer = new ValueComparer<List<StepConditionDef>>(
            (c1, c2) => c1 == null && c2 == null || (c1 != null && c2 != null && JsonSerializer.Serialize(c1) == JsonSerializer.Serialize(c2)),
            c => c == null ? 0 : JsonSerializer.Serialize(c).GetHashCode(),
            c => c == null ? new List<StepConditionDef>() : JsonSerializer.Deserialize<List<StepConditionDef>>(JsonSerializer.Serialize(c), (JsonSerializerOptions?)null) ?? new List<StepConditionDef>());

        modelBuilder.Entity<FormStep>()
            .Property(s => s.Conditions)
            .IsRequired(false)
            .HasConversion(
                v => JsonSerializer.Serialize(v ?? new List<StepConditionDef>(), (JsonSerializerOptions?)null),
                v => string.IsNullOrEmpty(v) ? new List<StepConditionDef>() : JsonSerializer.Deserialize<List<StepConditionDef>>(v, (JsonSerializerOptions?)null) ?? new List<StepConditionDef>())
            .Metadata.SetValueComparer(stepCondListComparer);

        modelBuilder.Entity<HelpArticle>()
            .HasKey(a => a.Id).IsClustered();

        modelBuilder.Entity<BrandingSettings>()
            .HasKey(b => b.Id).IsClustered();

        modelBuilder.Entity<UserAccount>()
            .HasKey(u => u.Id);

        modelBuilder.Entity<UserAccount>()
            .HasIndex(u => u.Email).IsUnique();

        modelBuilder.Entity<Company>()
            .HasKey(c => c.Id);

        modelBuilder.Entity<Company>()
            .HasIndex(c => c.FCANumber).IsUnique();

        modelBuilder.Entity<Company>()
            .HasMany(c => c.Brokers)
            .WithOne(u => u.Company)
            .HasForeignKey(u => u.CompanyId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Company>()
            .HasOne(c => c.ParentCompany)
            .WithMany(c => c.ChildCompanies)
            .HasForeignKey(c => c.ParentCompanyId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Address>()
            .HasKey(a => a.Id);

        modelBuilder.Entity<Address>()
            .HasOne(a => a.Company)
            .WithMany(c => c.Addresses)
            .HasForeignKey(a => a.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TradingName>()
            .HasKey(t => t.Id);

        modelBuilder.Entity<TradingName>()
            .HasOne(t => t.Company)
            .WithMany(c => c.TradingNames)
            .HasForeignKey(t => t.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<IntegrationSetting>()
            .HasKey(i => i.Id);

        modelBuilder.Entity<IntegrationSetting>()
            .HasIndex(i => new { i.Integration, i.Key }).IsUnique();

        modelBuilder.Entity<BankDetails>()
            .HasKey(b => b.Id);

        modelBuilder.Entity<BankDetails>()
            .HasOne(b => b.Company)
            .WithMany(c => c.BankAccounts)
            .HasForeignKey(b => b.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        // Workflows
        modelBuilder.Entity<Workflow>()
            .HasMany(w => w.Stages)
            .WithOne(s => s.Workflow)
            .HasForeignKey(s => s.WorkflowId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<WorkflowStage>()
            .HasMany(s => s.Tasks)
            .WithOne(t => t.Stage)
            .HasForeignKey(t => t.StageId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<WorkflowStage>()
            .HasMany(s => s.TransitionsOut)
            .WithOne(t => t.FromStage)
            .HasForeignKey(t => t.FromStageId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<WorkflowTransition>()
            .HasOne(t => t.ToStage)
            .WithMany()
            .HasForeignKey(t => t.ToStageId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<WorkflowTransition>()
            .HasOne<Workflow>()
            .WithMany()
            .HasForeignKey(t => t.WorkflowId)
            .OnDelete(DeleteBehavior.Cascade);

        // Applications
        modelBuilder.Entity<Application>()
            .HasKey(a => a.Id);

        modelBuilder.Entity<Application>()
            .HasIndex(a => a.PublicReference).IsUnique();

        modelBuilder.Entity<Application>()
            .HasOne(a => a.FormSchema)
            .WithMany()
            .HasForeignKey(a => a.FormSchemaId)
            .OnDelete(DeleteBehavior.Restrict);

        // NoAction on all user/company FKs: SQL Server rejects multiple SET NULL cascade
        // paths from the same principal table to the same dependent table.
        modelBuilder.Entity<Application>()
            .HasOne(a => a.Client)
            .WithMany()
            .HasForeignKey(a => a.ClientId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Application>()
            .HasOne(a => a.Broker)
            .WithMany()
            .HasForeignKey(a => a.BrokerId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Application>()
            .HasOne(a => a.Company)
            .WithMany()
            .HasForeignKey(a => a.CompanyId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Application>()
            .HasOne(a => a.Network)
            .WithMany()
            .HasForeignKey(a => a.NetworkId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Application>()
            .HasOne(a => a.Workflow)
            .WithMany()
            .HasForeignKey(a => a.WorkflowId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Application>()
            .HasOne(a => a.CurrentStage)
            .WithMany()
            .HasForeignKey(a => a.CurrentStageId)
            .OnDelete(DeleteBehavior.NoAction);

        // Royal Mail address field lengths (enforced at DB level)
        var addr = modelBuilder.Entity<Address>();
        addr.Property(a => a.OrganisationName).HasMaxLength(60);
        addr.Property(a => a.DepartmentName).HasMaxLength(60);
        addr.Property(a => a.SubBuildingName).HasMaxLength(30);
        addr.Property(a => a.BuildingName).HasMaxLength(50);
        addr.Property(a => a.BuildingNumber).HasMaxLength(12);
        addr.Property(a => a.DependentThoroughfareName).HasMaxLength(60);
        addr.Property(a => a.DependentThoroughfareDescriptor).HasMaxLength(20);
        addr.Property(a => a.ThoroughfareName).HasMaxLength(60);
        addr.Property(a => a.ThoroughfareDescriptor).HasMaxLength(20);
        addr.Property(a => a.DoubleDependentLocality).HasMaxLength(35);
        addr.Property(a => a.DependentLocality).HasMaxLength(35);
        addr.Property(a => a.PostTown).HasMaxLength(30);
        addr.Property(a => a.Postcode).HasMaxLength(8);
        addr.Property(a => a.POBox).HasMaxLength(6);
        addr.Property(a => a.Country).HasMaxLength(60);

        // Business rules
        modelBuilder.Entity<BusinessRule>()
            .HasOne(r => r.FormSchema)
            .WithMany()
            .HasForeignKey(r => r.FormSchemaId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<BusinessRule>()
            .Property(r => r.DecisionType)
            .HasMaxLength(20)
            .HasDefaultValue("Decline");

        modelBuilder.Entity<BusinessRule>()
            .HasMany(r => r.Conditions)
            .WithOne()
            .HasForeignKey(c => c.BusinessRuleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RuleOutcome>()
            .HasOne(o => o.BusinessRule)
            .WithMany()
            .HasForeignKey(o => o.BusinessRuleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RuleOutcome>()
            .HasOne<Application>()
            .WithMany()
            .HasForeignKey(o => o.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ChecklistItem>()
            .HasKey(i => i.Id);

        modelBuilder.Entity<ChecklistItem>()
            .HasOne(i => i.FormSchema)
            .WithMany()
            .HasForeignKey(i => i.FormSchemaId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ChecklistItem>()
            .HasMany(i => i.Conditions)
            .WithOne()
            .HasForeignKey(c => c.ChecklistItemId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ChecklistCondition>()
            .HasKey(c => c.Id);

        modelBuilder.Entity<ApplicationChecklistItem>()
            .HasKey(a => a.Id);

        modelBuilder.Entity<ApplicationChecklistItem>()
            .HasOne(a => a.ChecklistItem)
            .WithMany()
            .HasForeignKey(a => a.ChecklistItemId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ApplicationChecklistItem>()
            .HasOne<Application>()
            .WithMany()
            .HasForeignKey(a => a.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ApplicationChecklistComment>()
            .HasKey(c => c.Id);

        modelBuilder.Entity<ApplicationChecklistComment>()
            .HasOne<ApplicationChecklistItem>()
            .WithMany(a => a.Comments)
            .HasForeignKey(c => c.ApplicationChecklistItemId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Template>()
            .HasKey(t => t.Id);

        modelBuilder.Entity<Template>()
            .Property(t => t.TemplateType)
            .HasMaxLength(20);

        modelBuilder.Entity<ApplicationNote>()
            .HasKey(n => n.Id);

        modelBuilder.Entity<ApplicationNote>()
            .HasOne<Application>()
            .WithMany()
            .HasForeignKey(n => n.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ApplicationNote>()
            .Property(n => n.AuthorRole)
            .HasMaxLength(20);

        modelBuilder.Entity<LoginEvent>()
            .HasKey(e => e.Id);

        modelBuilder.Entity<LoginEvent>()
            .Property(e => e.UserType).HasMaxLength(30);

        modelBuilder.Entity<LoginEvent>()
            .Property(e => e.DeviceType).HasMaxLength(20);

        modelBuilder.Entity<LoginEvent>()
            .Property(e => e.AuthMethod).HasMaxLength(20);
    }
}
