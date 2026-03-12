namespace ChinhNha.Application.DTOs.Products;

public class ProductReviewDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? UserAvatarUrl { get; set; }
    
    public int Rating { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    
    public int HelpfulCount { get; set; }
    public int UnhelpfulCount { get; set; }
    
    public bool IsVerifiedPurchase { get; set; }
    public bool IsApproved { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateProductReviewDto
{
    public int ProductId { get; set; }
    public int Rating { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public class ProductReviewStatsDto
{
    public double AverageRating { get; set; }
    public int TotalReviews { get; set; }
    public int RatingCount1Star { get; set; }
    public int RatingCount2Star { get; set; }
    public int RatingCount3Star { get; set; }
    public int RatingCount4Star { get; set; }
    public int RatingCount5Star { get; set; }
    
    public Dictionary<int, double> RatingPercentages
    {
        get
        {
            if (TotalReviews == 0)
                return new() { { 1, 0 }, { 2, 0 }, { 3, 0 }, { 4, 0 }, { 5, 0 } };

            return new()
            {
                { 1, (RatingCount1Star * 100.0) / TotalReviews },
                { 2, (RatingCount2Star * 100.0) / TotalReviews },
                { 3, (RatingCount3Star * 100.0) / TotalReviews },
                { 4, (RatingCount4Star * 100.0) / TotalReviews },
                { 5, (RatingCount5Star * 100.0) / TotalReviews }
            };
        }
    }
}
