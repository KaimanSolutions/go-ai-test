using Xunit;

namespace FormBuilder.Backend.Tests;

public class UserAccountServiceTests
{
    // ── Register ──────────────────────────────────────────────────────────────

    [Fact]
    public void Register_ValidClient_CreatesAccount()
    {
        using var db = TestDb.Create();
        var svc = new UserAccountService(db);

        var (success, _) = svc.Register(new RegistrationRequest
        {
            Email     = "alice@test.com",
            Password  = "Password1!",
            FirstName = "Alice",
            LastName  = "Smith",
            Role      = UserRoles.Client
        });

        Assert.True(success);
        Assert.Single(db.UserAccounts.ToList());
        Assert.Equal("alice@test.com", db.UserAccounts.First().Email);
    }

    [Fact]
    public void Register_DuplicateEmail_ReturnsError()
    {
        using var db = TestDb.Create();
        TestDb.SeedUser(db, UserRoles.Client, "alice@test.com");
        var svc = new UserAccountService(db);

        var (success, error) = svc.Register(new RegistrationRequest
        {
            Email     = "alice@test.com",
            Password  = "Password1!",
            FirstName = "Alice",
            LastName  = "Smith",
            Role      = UserRoles.Client
        });

        Assert.False(success);
        Assert.Contains("already exists", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Register_PasswordTooShort_ReturnsError()
    {
        using var db = TestDb.Create();
        var svc = new UserAccountService(db);

        var (success, error) = svc.Register(new RegistrationRequest
        {
            Email     = "bob@test.com",
            Password  = "abc",
            FirstName = "Bob",
            LastName  = "Jones",
            Role      = UserRoles.Client
        });

        Assert.False(success);
        Assert.Contains("8 characters", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Register_MissingEmail_ReturnsError()
    {
        using var db = TestDb.Create();
        var svc = new UserAccountService(db);

        var (success, error) = svc.Register(new RegistrationRequest
        {
            Email     = "   ",
            Password  = "Password1!",
            FirstName = "Bob",
            LastName  = "Jones",
            Role      = UserRoles.Client
        });

        Assert.False(success);
        Assert.NotEmpty(error);
    }

    [Fact]
    public void Register_MissingFirstOrLastName_ReturnsError()
    {
        using var db = TestDb.Create();
        var svc = new UserAccountService(db);

        var (success, error) = svc.Register(new RegistrationRequest
        {
            Email     = "bob@test.com",
            Password  = "Password1!",
            FirstName = "",
            LastName  = "Jones",
            Role      = UserRoles.Client
        });

        Assert.False(success);
        Assert.Contains("name", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Register_BrokerWithoutCompany_ReturnsError()
    {
        using var db = TestDb.Create();
        var svc = new UserAccountService(db);

        var (success, error) = svc.Register(new RegistrationRequest
        {
            Email     = "broker@test.com",
            Password  = "Password1!",
            FirstName = "Bob",
            LastName  = "Broker",
            Role      = UserRoles.Broker
            // CompanyId intentionally omitted
        });

        Assert.False(success);
        Assert.Contains("company", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Register_BrokerWithNonExistentCompany_ReturnsError()
    {
        using var db = TestDb.Create();
        var svc = new UserAccountService(db);

        var (success, error) = svc.Register(new RegistrationRequest
        {
            Email     = "broker@test.com",
            Password  = "Password1!",
            FirstName = "Bob",
            LastName  = "Broker",
            Role      = UserRoles.Broker,
            CompanyId = 9999
        });

        Assert.False(success);
        Assert.Contains("not exist", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Register_BrokerWithValidCompany_Succeeds()
    {
        using var db = TestDb.Create();
        var company = TestDb.SeedCompany(db);
        var svc = new UserAccountService(db);

        var (success, _) = svc.Register(new RegistrationRequest
        {
            Email     = "broker@test.com",
            Password  = "Password1!",
            FirstName = "Bob",
            LastName  = "Broker",
            Role      = UserRoles.Broker,
            CompanyId = company.Id
        });

        Assert.True(success);
        Assert.Equal(company.Id, db.UserAccounts.First().CompanyId);
    }

    [Fact]
    public void Register_AdminRoleViaPublicEndpoint_ReturnsError()
    {
        using var db = TestDb.Create();
        var svc = new UserAccountService(db);

        var (success, error) = svc.Register(new RegistrationRequest
        {
            Email     = "admin@test.com",
            Password  = "Password1!",
            FirstName = "Admin",
            LastName  = "User",
            Role      = UserRoles.Admin
        });

        Assert.False(success);
        Assert.Contains("Invalid role", error, StringComparison.OrdinalIgnoreCase);
    }

    // ── AdminCreateUser ───────────────────────────────────────────────────────

    [Fact]
    public void AdminCreateUser_AdminRole_Succeeds()
    {
        using var db = TestDb.Create();
        var svc = new UserAccountService(db);

        var (success, _) = svc.AdminCreateUser(new RegistrationRequest
        {
            Email     = "newadmin@test.com",
            Password  = "Password1!",
            FirstName = "New",
            LastName  = "Admin",
            Role      = UserRoles.Admin
        });

        Assert.True(success);
        Assert.Equal(UserRoles.Admin, db.UserAccounts.First().Role);
    }

    [Fact]
    public void AdminCreateUser_InvalidRole_ReturnsError()
    {
        using var db = TestDb.Create();
        var svc = new UserAccountService(db);

        var (success, error) = svc.AdminCreateUser(new RegistrationRequest
        {
            Email     = "x@test.com",
            Password  = "Password1!",
            FirstName = "X",
            LastName  = "Y",
            Role      = "SuperAdmin"
        });

        Assert.False(success);
        Assert.NotEmpty(error);
    }

    // ── SetLockout ────────────────────────────────────────────────────────────

    [Fact]
    public void SetLockout_True_LocksUser()
    {
        using var db = TestDb.Create();
        var user = TestDb.SeedUser(db, UserRoles.Client);
        var svc  = new UserAccountService(db);

        var (success, _) = svc.SetLockout(user.Id, true);

        Assert.True(success);
        Assert.True(db.UserAccounts.Find(user.Id)!.IsLockedOut);
    }

    [Fact]
    public void SetLockout_False_UnlocksUser()
    {
        using var db = TestDb.Create();
        var user = TestDb.SeedUser(db, UserRoles.Client);
        user.IsLockedOut = true;
        db.SaveChanges();
        var svc = new UserAccountService(db);

        svc.SetLockout(user.Id, false);

        Assert.False(db.UserAccounts.Find(user.Id)!.IsLockedOut);
    }

    [Fact]
    public void SetLockout_NonExistentUser_ReturnsError()
    {
        using var db = TestDb.Create();
        var svc = new UserAccountService(db);

        var (success, error) = svc.SetLockout(9999, true);

        Assert.False(success);
        Assert.Contains("not found", error, StringComparison.OrdinalIgnoreCase);
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [Fact]
    public void Delete_ExistingUser_IsRemoved()
    {
        using var db = TestDb.Create();
        var user = TestDb.SeedUser(db, UserRoles.Client);
        var svc  = new UserAccountService(db);

        var (success, _) = svc.Delete(user.Id);

        Assert.True(success);
        Assert.Empty(db.UserAccounts.ToList());
    }

    [Fact]
    public void Delete_NonExistentUser_ReturnsError()
    {
        using var db = TestDb.Create();
        var svc = new UserAccountService(db);

        var (success, error) = svc.Delete(9999);

        Assert.False(success);
        Assert.Contains("not found", error, StringComparison.OrdinalIgnoreCase);
    }

    // ── ValidateCredentials ───────────────────────────────────────────────────

    [Fact]
    public void ValidateCredentials_CorrectPassword_ReturnsUser()
    {
        using var db = TestDb.Create();
        var svc = new UserAccountService(db);
        svc.Register(new RegistrationRequest
        {
            Email = "alice@test.com", Password = "MyPassword1!",
            FirstName = "Alice", LastName = "Smith", Role = UserRoles.Client
        });

        var result = svc.ValidateCredentials("alice@test.com", "MyPassword1!");

        Assert.NotNull(result);
        Assert.Equal("alice@test.com", result!.Email);
    }

    [Fact]
    public void ValidateCredentials_WrongPassword_ReturnsNull()
    {
        using var db = TestDb.Create();
        var svc = new UserAccountService(db);
        svc.Register(new RegistrationRequest
        {
            Email = "alice@test.com", Password = "MyPassword1!",
            FirstName = "Alice", LastName = "Smith", Role = UserRoles.Client
        });

        var result = svc.ValidateCredentials("alice@test.com", "WrongPassword!");

        Assert.Null(result);
    }

    [Fact]
    public void ValidateCredentials_LockedOutUser_ReturnsNull()
    {
        using var db = TestDb.Create();
        var svc = new UserAccountService(db);
        svc.Register(new RegistrationRequest
        {
            Email = "alice@test.com", Password = "MyPassword1!",
            FirstName = "Alice", LastName = "Smith", Role = UserRoles.Client
        });
        var user = db.UserAccounts.First();
        user.IsLockedOut = true;
        db.SaveChanges();

        var result = svc.ValidateCredentials("alice@test.com", "MyPassword1!");

        Assert.Null(result);
    }

    [Fact]
    public void ValidateCredentials_UnknownUser_ReturnsNull()
    {
        using var db = TestDb.Create();
        var svc = new UserAccountService(db);

        var result = svc.ValidateCredentials("nobody@test.com", "Password1!");

        Assert.Null(result);
    }

    // ── UpdateProfileByEmail ──────────────────────────────────────────────────

    [Fact]
    public void UpdateProfile_ValidEmail_UpdatesFields()
    {
        using var db = TestDb.Create();
        TestDb.SeedUser(db, UserRoles.Client, "alice@test.com");
        var svc = new UserAccountService(db);

        var (success, _, profile) = svc.UpdateProfileByEmail("alice@test.com", new UpdateProfileRequest
        {
            FirstName  = "Alicia",
            JobTitle   = "Senior Analyst",
            Department = "Risk"
        });

        Assert.True(success);
        Assert.Equal("Alicia",          profile!.FirstName);
        Assert.Equal("Senior Analyst",  profile.JobTitle);
        Assert.Equal("Risk",            profile.Department);
    }

    [Fact]
    public void UpdateProfile_NonExistentEmail_ReturnsError()
    {
        using var db = TestDb.Create();
        var svc = new UserAccountService(db);

        var (success, error, _) = svc.UpdateProfileByEmail("ghost@test.com", new UpdateProfileRequest
        {
            FirstName = "Ghost"
        });

        Assert.False(success);
        Assert.NotEmpty(error);
    }
}
