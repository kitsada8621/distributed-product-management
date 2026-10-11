namespace Auth.Infrastructure.Persistence;

public sealed class SeedOptions
{
    public string AdminEmail { get; set; } = string.Empty;

    public string AdminPassword { get; set; } = string.Empty;

    public string AdminName { get; set; } = "Administrator";
}
