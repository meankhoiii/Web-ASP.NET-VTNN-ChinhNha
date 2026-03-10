using ChinhNha.Domain.Interfaces;

namespace ChinhNha.Domain.Entities;

public class BlogPost : BaseEntity, IAggregateRoot
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? FeaturedImageUrl { get; set; }
    
    public int CategoryId { get; set; }
    public BlogCategory Category { get; set; } = null!;

    public string? AuthorId { get; set; }
    public AppUser? Author { get; set; }

    public int ViewCount { get; set; } = 0;
    
    public bool IsPublished { get; set; } = false;
    public DateTime? PublishedAt { get; set; }

    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
