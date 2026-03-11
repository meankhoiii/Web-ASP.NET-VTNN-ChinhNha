using ChinhNha.Application.DTOs.Blogs;

namespace ChinhNha.Application.Interfaces;

public interface IBlogService
{
    Task<IEnumerable<BlogPostDto>> GetPublishedPostsAsync();
    Task<BlogPostDto?> GetPostBySlugAsync(string slug);
    Task<IEnumerable<BlogCategoryDto>> GetCategoriesAsync();
}
