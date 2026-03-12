namespace ChinhNha.Domain.Entities;

public class AppUserRole
{
    public string UserId { get; set; } = string.Empty;
    public AppUser User { get; set; } = null!;

    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;
}
