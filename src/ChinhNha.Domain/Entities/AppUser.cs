namespace ChinhNha.Domain.Entities;

public class AppUser
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }
    
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<AppUserRole> UserRoles { get; set; } = new List<AppUserRole>();
}
