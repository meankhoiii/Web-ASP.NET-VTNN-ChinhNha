using ChinhNha.Domain.Entities;
using ChinhNha.Domain.Interfaces;
using ChinhNha.Infrastructure.Data;

namespace ChinhNha.Infrastructure.Repositories;

/// <summary>Product recommendation repository implementation</summary>
public class ProductRecommendationRepository : GenericRepository<ProductRecommendation>, IProductRecommendationRepository
{
    public ProductRecommendationRepository(AppDbContext context) : base(context)
    {
    }

    /// <summary>Get recommendations for a user</summary>
    public async Task<IEnumerable<ProductRecommendation>> GetUserRecommendationsAsync(string userId, int limit = 10)
    {
        var all = await ListAllAsync();
        return all
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.RecommendationScore)
            .ThenByDescending(r => r.CreatedAt)
            .Take(limit)
            .ToList();
    }

    /// <summary>Get recommendations by type</summary>
    public async Task<IEnumerable<ProductRecommendation>> GetRecommendationsByTypeAsync(string userId, string recommendationType, int limit = 10)
    {
        var all = await ListAllAsync();
        return all
            .Where(r => r.UserId == userId && r.RecommendationType == recommendationType)
            .OrderByDescending(r => r.RecommendationScore)
            .Take(limit)
            .ToList();
    }

    /// <summary>Mark recommendation as viewed</summary>
    public async Task MarkAsViewedAsync(int recommendationId)
    {
        var rec = await GetByIdAsync(recommendationId);
        if (rec == null) return;

        rec.ViewedAt = DateTime.UtcNow;
        if (rec.ConversionValue == 0)
            rec.ConversionValue = 1;

        await UpdateAsync(rec);
    }

    /// <summary>Mark recommendation as clicked</summary>
    public async Task MarkAsClickedAsync(int recommendationId)
    {
        var rec = await GetByIdAsync(recommendationId);
        if (rec == null) return;

        rec.IsClicked = true;
        rec.ClickedAt = DateTime.UtcNow;
        if (rec.ConversionValue < 2)
            rec.ConversionValue = 2;

        await UpdateAsync(rec);
    }

    /// <summary>Mark recommendation as added to cart</summary>
    public async Task MarkAsAddedToCartAsync(int recommendationId)
    {
        var rec = await GetByIdAsync(recommendationId);
        if (rec == null) return;

        rec.IsAddedToCart = true;
        if (rec.ConversionValue < 2)
            rec.ConversionValue = 2;

        await UpdateAsync(rec);
    }

    /// <summary>Mark recommendation as purchased</summary>
    public async Task MarkAsPurchasedAsync(int recommendationId)
    {
        var rec = await GetByIdAsync(recommendationId);
        if (rec == null) return;

        rec.IsPurchased = true;
        rec.PurchasedAt = DateTime.UtcNow;
        rec.ConversionValue = 3;

        await UpdateAsync(rec);
    }

    /// <summary>Save new recommendation(s)</summary>
    public async Task SaveRecommendationsAsync(IEnumerable<ProductRecommendation> recommendations)
    {
        foreach (var rec in recommendations)
        {
            await AddAsync(rec);
        }
    }

    /// <summary>Get recommendation effectiveness stats</summary>
    public async Task<(int TotalShown, int Viewed, int Clicked, int CartAdds, int Purchases)> GetRecommendationStatsAsync(string? userId = null, int days = 30)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-days);
        var all = await ListAllAsync();
        
        var filtered = userId != null
            ? all.Where(r => r.UserId == userId && r.CreatedAt >= cutoffDate).ToList()
            : all.Where(r => r.CreatedAt >= cutoffDate).ToList();

        int totalShown = filtered.Count(r => r.IsShown);
        int viewed = filtered.Count(r => r.ViewedAt.HasValue);
        int clicked = filtered.Count(r => r.IsClicked);
        int cartAdds = filtered.Count(r => r.IsAddedToCart);
        int purchases = filtered.Count(r => r.IsPurchased);

        return (totalShown, viewed, clicked, cartAdds, purchases);
    }

    /// <summary>Get recommendations by reason</summary>
    public async Task<IEnumerable<ProductRecommendation>> GetRecommendationsByReasonAsync(string reason, int limit = 10)
    {
        var all = await ListAllAsync();
        return all
            .Where(r => r.RecommendationReason == reason)
            .OrderByDescending(r => r.RecommendationScore)
            .Take(limit)
            .ToList();
    }

    /// <summary>Delete old recommendations</summary>
    public async Task<int> DeleteOldRecommendationsAsync(int daysOld = 90)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-daysOld);
        var all = await ListAllAsync();
        var toDelete = all.Where(r => r.CreatedAt < cutoffDate).ToList();

        foreach (var rec in toDelete)
        {
            await DeleteAsync(rec);
        }

        return toDelete.Count;
    }

    /// <summary>Get products frequently recommended</summary>
    public async Task<IEnumerable<(int ProductId, int RecommendationCount)>> GetMostRecommendedProductsAsync(int limit = 10, int days = 30)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-days);
        var all = await ListAllAsync();
        
        return all
            .Where(r => r.CreatedAt >= cutoffDate)
            .GroupBy(r => r.RecommendedProductId)
            .Select(g => (g.Key, g.Count()))
            .OrderByDescending(x => x.Item2)
            .Take(limit)
            .ToList();
    }
}
