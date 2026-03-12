using ChinhNha.Application.DTOs.Products;

namespace ChinhNha.Application.Interfaces;

public interface IProductReviewService
{
    Task<ProductReviewDto?> GetReviewByIdAsync(int reviewId);
    
    Task<IEnumerable<ProductReviewDto>> GetProductReviewsAsync(int productId, int pageNumber = 1, int pageSize = 10, bool onlyApproved = true);
    
    Task<ProductReviewStatsDto> GetProductReviewStatsAsync(int productId);
    
    Task<ProductReviewDto?> CreateReviewAsync(string userId, int productId, int rating, string title, string content);
    
    Task<bool> UpdateReviewAsync(int reviewId, string userId, int rating, string title, string content);
    
    Task<bool> DeleteReviewAsync(int reviewId, string userId);
    
    Task<bool> MarkHelpfulAsync(int reviewId, string userId);
    
    Task<bool> MarkUnhelpfulAsync(int reviewId, string userId);
    
    Task<bool> ApproveReviewAsync(int reviewId);
    
    Task<bool> RejectReviewAsync(int reviewId);
    
    Task<bool> CanUserReviewProductAsync(string userId, int productId);
}
