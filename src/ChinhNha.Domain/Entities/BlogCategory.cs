using ChinhNha.Domain.Interfaces;

namespace ChinhNha.Domain.Entities;

public class BlogCategory : BaseEntity, IAggregateRoot
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    
    public int DisplayOrder { get; set; } = 0;
    
    public ICollection<BlogPost> Posts { get; set; } = new List<BlogPost>();
}
