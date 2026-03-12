using ChinhNha.Application.DTOs.Products;
using ChinhNha.Application.Interfaces;
using ChinhNha.Domain.Entities;
using ChinhNha.Domain.Interfaces;
using AutoMapper;

namespace ChinhNha.Application.Services;

public class ProductReviewService : IProductReviewService
{
    private readonly IRepository<ProductReview> _reviewRepository;
    private readonly IProductRepository _productRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IAppUserService _userService;
    private readonly IMapper _mapper;

    public ProductReviewService(
        IRepository<ProductReview> reviewRepository,
        IProductRepository productRepository,
        IOrderRepository orderRepository,
        IAppUserService userService,
        IMapper mapper)
    {
        _reviewRepository = reviewRepository;
        _productRepository = productRepository;
        _orderRepository = orderRepository;
        _userService = userService;
        _mapper = mapper;
    }

    public async Task<ProductReviewDto?> GetReviewByIdAsync(int reviewId)
    {
        var review = await _reviewRepository.GetByIdAsync(reviewId);
        if (review == null) return null;

        var user = await _userService.GetUserByIdAsync(review.UserId);
        return MapToDto(review, user);
    }

    public async Task<IEnumerable<ProductReviewDto>> GetProductReviewsAsync(
        int productId, int pageNumber = 1, int pageSize = 10, bool onlyApproved = true)
    {
        // Get all reviews for product from DB
        var allReviews = await _reviewRepository.ListAllAsync();
        var filtered = allReviews
            .Where(r => r.ProductId == productId && (!onlyApproved || r.IsApproved))
            .OrderByDescending(r => r.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var result = new List<ProductReviewDto>();
        foreach (var review in filtered)
        {
            var user = await _userService.GetUserByIdAsync(review.UserId);
            result.Add(MapToDto(review, user));
        }

        return result;
    }

    public async Task<ProductReviewStatsDto> GetProductReviewStatsAsync(int productId)
    {
        var allReviews = await _reviewRepository.ListAllAsync();
        var reviews = allReviews.Where(r => r.ProductId == productId && r.IsApproved).ToList();

        var stats = new ProductReviewStatsDto
        {
            TotalReviews = reviews.Count(),
            AverageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0,
            RatingCount1Star = reviews.Count(r => r.Rating == 1),
            RatingCount2Star = reviews.Count(r => r.Rating == 2),
            RatingCount3Star = reviews.Count(r => r.Rating == 3),
            RatingCount4Star = reviews.Count(r => r.Rating == 4),
            RatingCount5Star = reviews.Count(r => r.Rating == 5)
        };

        return stats;
    }

    public async Task<ProductReviewDto?> CreateReviewAsync(
        string userId, int productId, int rating, string title, string content)
    {
        if (rating < 1 || rating > 5)
            throw new ArgumentException("Rating must be between 1 and 5");

        // Verify user can review this product
        if (!await CanUserReviewProductAsync(userId, productId))
            return null;

        var product = await _productRepository.GetByIdAsync(productId);
        if (product == null) return null;

        // Check if user already reviewed this product
        var allReviews = await _reviewRepository.ListAllAsync();
        if (allReviews.Any(r => r.ProductId == productId && r.UserId == userId))
            return null; // User already has a review

        var review = new ProductReview
        {
            ProductId = productId,
            UserId = userId,
            Rating = rating,
            Title = title,
            Content = content,
            IsVerifiedPurchase = await IsVerifiedPurchaseAsync(userId, productId),
            IsApproved = true // Auto-approve for now
        };

        await _reviewRepository.AddAsync(review);

        var user = await _userService.GetUserByIdAsync(userId);
        return MapToDto(review, user);
    }

    public async Task<bool> UpdateReviewAsync(int reviewId, string userId, int rating, string title, string content)
    {
        var review = await _reviewRepository.GetByIdAsync(reviewId);
        if (review == null || review.UserId != userId)
            return false;

        if (rating < 1 || rating > 5)
            return false;

        review.Rating = rating;
        review.Title = title;
        review.Content = content;
        review.UpdatedAt = DateTime.UtcNow;

        await _reviewRepository.UpdateAsync(review);
        return true;
    }

    public async Task<bool> DeleteReviewAsync(int reviewId, string userId)
    {
        var review = await _reviewRepository.GetByIdAsync(reviewId);
        if (review == null || review.UserId != userId)
            return false;

        await _reviewRepository.DeleteAsync(review);
        return true;
    }

    public async Task<bool> MarkHelpfulAsync(int reviewId, string userId)
    {
        var review = await _reviewRepository.GetByIdAsync(reviewId);
        if (review == null) return false;

        review.HelpfulCount++;
        await _reviewRepository.UpdateAsync(review);
        return true;
    }

    public async Task<bool> MarkUnhelpfulAsync(int reviewId, string userId)
    {
        var review = await _reviewRepository.GetByIdAsync(reviewId);
        if (review == null) return false;

        review.UnhelpfulCount++;
        await _reviewRepository.UpdateAsync(review);
        return true;
    }

    public async Task<bool> ApproveReviewAsync(int reviewId)
    {
        var review = await _reviewRepository.GetByIdAsync(reviewId);
        if (review == null) return false;

        review.IsApproved = true;
        await _reviewRepository.UpdateAsync(review);
        return true;
    }

    public async Task<bool> RejectReviewAsync(int reviewId)
    {
        var review = await _reviewRepository.GetByIdAsync(reviewId);
        if (review == null) return false;

        await _reviewRepository.DeleteAsync(review);
        return true;
    }

    public async Task<bool> CanUserReviewProductAsync(string userId, int productId)
    {
        // User must have purchased the product
        return await IsVerifiedPurchaseAsync(userId, productId);
    }

    private async Task<bool> IsVerifiedPurchaseAsync(string userId, int productId)
    {
        var orders = await _orderRepository.GetUserOrdersWithDetailsAsync(userId);
        return orders.Any(o => o.OrderItems.Any(oi => oi.ProductId == productId));
    }

    private ProductReviewDto MapToDto(ProductReview review, AppUser? user)
    {
        return new ProductReviewDto
        {
            Id = review.Id,
            ProductId = review.ProductId,
            UserId = review.UserId,
            UserName = user?.FullName ?? "Anonymous",
            UserAvatarUrl = user?.AvatarUrl,
            Rating = review.Rating,
            Title = review.Title,
            Content = review.Content,
            HelpfulCount = review.HelpfulCount,
            UnhelpfulCount = review.UnhelpfulCount,
            IsVerifiedPurchase = review.IsVerifiedPurchase,
            IsApproved = review.IsApproved,
            CreatedAt = review.CreatedAt,
            UpdatedAt = review.UpdatedAt
        };
    }
}
