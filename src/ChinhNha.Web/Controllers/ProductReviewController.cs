using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ChinhNha.Application.Interfaces;
using ChinhNha.Application.DTOs.Products;

namespace ChinhNha.Web.Controllers;

public class ProductReviewController : Controller
{
    private readonly IProductReviewService _reviewService;
    private readonly IProductService _productService;
    private readonly ILogger<ProductReviewController> _logger;

    public ProductReviewController(
        IProductReviewService reviewService,
        IProductService productService,
        ILogger<ProductReviewController> logger)
    {
        _reviewService = reviewService;
        _productService = productService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetReviews(int productId, int page = 1)
    {
        try
        {
            var product = await _productService.GetProductByIdAsync(productId);
            if (product == null)
                return NotFound();

            var reviews = await _reviewService.GetProductReviewsAsync(productId, page, 10);
            var stats = await _reviewService.GetProductReviewStatsAsync(productId);

            ViewBag.ProductId = productId;
            ViewBag.ProductName = product.Name;
            ViewBag.ReviewStats = stats;

            return PartialView("_ReviewsList", reviews);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reviews for product {ProductId}", productId);
            return BadRequest("Error loading reviews");
        }
    }

    [HttpGet]
    [Route("Product/{productId}/Reviews")]
    public async Task<IActionResult> ProductReviews(int productId, int page = 1)
    {
        try
        {
            var product = await _productService.GetProductByIdAsync(productId);
            if (product == null)
                return NotFound();

            var reviews = await _reviewService.GetProductReviewsAsync(productId, page, 10);
            var stats = await _reviewService.GetProductReviewStatsAsync(productId);

            ViewBag.ProductId = productId;
            ViewBag.ProductName = product.Name;
            ViewBag.ReviewStats = stats;
            ViewBag.CurrentPage = page;

            return View(reviews);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading product reviews");
            return BadRequest("Error loading reviews");
        }
    }

    [HttpPost]
    [Authorize]
    [Route("ProductReview/Create")]
    public async Task<IActionResult> CreateReview([FromForm] CreateProductReviewDto model)
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            // Validate input
            if (model.Rating < 1 || model.Rating > 5)
                return BadRequest("Rating must be between 1-5 stars");

            if (string.IsNullOrWhiteSpace(model.Title) || model.Title.Length > 200)
                return BadRequest("Title must be provided and less than 200 characters");

            if (string.IsNullOrWhiteSpace(model.Content) || model.Content.Length < 10 || model.Content.Length > 2000)
                return BadRequest("Review must be between 10-2000 characters");

            var result = await _reviewService.CreateReviewAsync(
                userId, model.ProductId, model.Rating, model.Title, model.Content);

            if (result == null)
            {
                TempData["ErrorMessage"] = "Bạn không thể đánh giá sản phẩm này hoặc đã từng đánh giá rồi.";
                return RedirectToAction("ProductReviews", new { productId = model.ProductId });
            }

            TempData["SuccessMessage"] = "Đánh giá của bạn đã được gửi thành công!";
            return RedirectToAction("ProductReviews", new { productId = model.ProductId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product review");
            TempData["ErrorMessage"] = "Lỗi khi gửi đánh giá. Vui lòng thử lại.";
            return RedirectToAction("ProductReviews", new { productId = model.ProductId });
        }
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> MarkHelpful(int reviewId, int productId)
    {
        try
        {
            await _reviewService.MarkHelpfulAsync(reviewId, User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!);
            return Ok(new { success = true });
        }
        catch
        {
            return BadRequest("Error updating review");
        }
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> MarkUnhelpful(int reviewId, int productId)
    {
        try
        {
            await _reviewService.MarkUnhelpfulAsync(reviewId, User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!);
            return Ok(new { success = true });
        }
        catch
        {
            return BadRequest("Error updating review");
        }
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> DeleteReview(int reviewId, int productId)
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var result = await _reviewService.DeleteReviewAsync(reviewId, userId!);

            if (!result)
                return Unauthorized();

            TempData["SuccessMessage"] = "Đánh giá đã được xóa thành công.";
            return RedirectToAction("ProductReviews", new { productId = productId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting review");
            return BadRequest("Error deleting review");
        }
    }
}
