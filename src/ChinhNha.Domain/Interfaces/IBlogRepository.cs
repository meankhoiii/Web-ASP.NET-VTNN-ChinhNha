using ChinhNha.Domain.Entities;

namespace ChinhNha.Domain.Interfaces;

public interface IBlogRepository : IRepository<BlogPost>
{
    Task<IEnumerable<BlogPost>> GetAllPostsWithCategoryAsync();
    Task<IEnumerable<BlogPost>> GetPublishedPostsWithCategoryAsync();
    Task<BlogPost?> GetPostByIdWithCategoryAsync(int id);
    Task<BlogPost?> GetPostBySlugWithCategoryAsync(string slug);
}
