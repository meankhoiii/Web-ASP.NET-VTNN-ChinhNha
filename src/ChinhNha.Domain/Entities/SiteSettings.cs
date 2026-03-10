namespace ChinhNha.Domain.Entities;

public class SiteSettings : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
    public string? Group { get; set; } // General, Contact, Social, SEO
}
