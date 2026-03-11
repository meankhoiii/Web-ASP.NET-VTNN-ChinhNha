using AutoMapper;
using ChinhNha.Application.DTOs.Blogs;
using ChinhNha.Application.DTOs.Requests;
using ChinhNha.Application.Helpers;
using ChinhNha.Application.Interfaces;
using ChinhNha.Domain.Entities;
using ChinhNha.Domain.Interfaces;

namespace ChinhNha.Application.Services;

public class BlogService : IBlogService
{
    private readonly IBlogRepository _blogRepo;
    private readonly IRepository<BlogCategory> _categoryRepo;
    private readonly IMapper _mapper;

    public BlogService(IBlogRepository blogRepo, IRepository<BlogCategory> categoryRepo, IMapper mapper)
    {
        _blogRepo = blogRepo;
        _categoryRepo = categoryRepo;
        _mapper = mapper;
    }

    public async Task<IEnumerable<BlogPostDto>> GetAllPostsAsync()
    {
        var posts = await _blogRepo.GetAllPostsWithCategoryAsync();
        return _mapper.Map<IEnumerable<BlogPostDto>>(posts);
    }

    public async Task<IEnumerable<BlogPostDto>> GetPublishedPostsAsync()
    {
        var posts = await _blogRepo.GetPublishedPostsWithCategoryAsync();
        return _mapper.Map<IEnumerable<BlogPostDto>>(posts);
    }

    public async Task<BlogPostDto?> GetPostByIdAsync(int id)
    {
        var post = await _blogRepo.GetPostByIdWithCategoryAsync(id);
        return post == null ? null : _mapper.Map<BlogPostDto>(post);
    }

    public async Task<BlogPostDto?> GetPostBySlugAsync(string slug)
    {
        var post = await _blogRepo.GetPostBySlugWithCategoryAsync(slug);
        return post == null ? null : _mapper.Map<BlogPostDto>(post);
    }

    public async Task<IEnumerable<BlogCategoryDto>> GetCategoriesAsync()
    {
        var cats = await _categoryRepo.ListAllAsync();
        return _mapper.Map<IEnumerable<BlogCategoryDto>>(cats);
    }

    public async Task<int> CreatePostAsync(CreatePostRequest request, string? authorId)
    {
        var slug = SlugHelper.GenerateSlug(request.Title);
        var post = new BlogPost
        {
            Title = request.Title,
            Slug = slug,
            Content = request.Content,
            Summary = request.Excerpt,
            FeaturedImageUrl = request.ImageUrl,
            CategoryId = request.CategoryId,
            AuthorId = authorId,
            IsPublished = request.IsPublished,
            MetaTitle = request.MetaTitle,
            MetaDescription = request.MetaDescription,
            CreatedAt = DateTime.UtcNow,
            PublishedAt = request.IsPublished ? DateTime.UtcNow : null
        };

        await _blogRepo.AddAsync(post);
        return post.Id;
    }

    public async Task<bool> UpdatePostAsync(UpdatePostRequest request)
    {
        var post = await _blogRepo.GetByIdAsync(request.Id);
        if (post == null) return false;

        post.Title = request.Title;
        if (string.IsNullOrEmpty(post.Slug))
        {
            post.Slug = SlugHelper.GenerateSlug(request.Title);
        }
        
        post.Content = request.Content;
        post.Summary = request.Excerpt;
        post.FeaturedImageUrl = request.ImageUrl;
        post.CategoryId = request.CategoryId;
        post.MetaTitle = request.MetaTitle;
        post.MetaDescription = request.MetaDescription;
        post.UpdatedAt = DateTime.UtcNow;

        if (request.IsPublished && !post.IsPublished)
        {
            post.IsPublished = true;
            post.PublishedAt = post.PublishedAt ?? DateTime.UtcNow;
        }
        else if (!request.IsPublished)
        {
            post.IsPublished = false;
        }

        await _blogRepo.UpdateAsync(post);
        return true;
    }

    public async Task<bool> DeletePostAsync(int id)
    {
        var post = await _blogRepo.GetByIdAsync(id);
        if (post == null) return false;

        await _blogRepo.DeleteAsync(post);
        return true;
    }
}
