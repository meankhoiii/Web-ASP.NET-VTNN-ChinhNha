using ChinhNha.Domain.Entities;
using ChinhNha.Domain.Interfaces;
using ChinhNha.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ChinhNha.Infrastructure.Repositories;

public class BlogRepository : GenericRepository<BlogPost>, IBlogRepository
{
    public BlogRepository(AppDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<IEnumerable<BlogPost>> GetAllPostsWithCategoryAsync()
    {
        return await _dbContext.BlogPosts
            .Include(p => p.Category)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<BlogPost>> GetPublishedPostsWithCategoryAsync()
    {
        return await _dbContext.BlogPosts
            .Include(p => p.Category)
            .Where(p => p.IsPublished && p.PublishedAt <= DateTime.UtcNow)
            .OrderByDescending(p => p.PublishedAt)
            .ToListAsync();
    }

    public async Task<BlogPost?> GetPostByIdWithCategoryAsync(int id)
    {
        return await _dbContext.BlogPosts
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<BlogPost?> GetPostBySlugWithCategoryAsync(string slug)
    {
        return await _dbContext.BlogPosts
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Slug == slug);
    }
}
