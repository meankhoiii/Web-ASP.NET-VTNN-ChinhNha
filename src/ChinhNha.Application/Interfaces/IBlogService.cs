using ChinhNha.Application.DTOs.Blogs;
using ChinhNha.Application.DTOs.Requests;

namespace ChinhNha.Application.Interfaces;

public interface IBlogService
{
    Task<IEnumerable<BlogPostDto>> GetAllPostsAsync();
    Task<IEnumerable<BlogPostDto>> GetPublishedPostsAsync();
    Task<BlogPostDto?> GetPostByIdAsync(int id);
    Task<BlogPostDto?> GetPostBySlugAsync(string slug);
    Task<IEnumerable<BlogCategoryDto>> GetCategoriesAsync();

    Task<int> CreatePostAsync(CreatePostRequest request, string? authorId);
    Task<bool> UpdatePostAsync(UpdatePostRequest request);
    Task<bool> DeletePostAsync(int id);
}
