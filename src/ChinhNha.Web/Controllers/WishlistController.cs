using System.Security.Claims;
using ChinhNha.Application.DTOs.Products;
using ChinhNha.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChinhNha.Web.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class WishlistController : ControllerBase
{
    private readonly IWishlistService _wishlistService;
    private readonly ILogger<WishlistController> _logger;

    public WishlistController(
        IWishlistService wishlistService,
        ILogger<WishlistController> logger)
    {
        _wishlistService = wishlistService ?? throw new ArgumentNullException(nameof(wishlistService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<WishlistDto>>> GetUserWishlist()
    {
        try
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(new { message = "Vui lòng đăng nhập" });

            var result = await _wishlistService.GetUserWishlistsAsync(userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user wishlist");
            return BadRequest(new { message = "Lỗi khi lấy danh sách yêu thích" });
        }
    }

    [HttpPost]
    public async Task<ActionResult<WishlistDto>> AddToWishlist([FromBody] CreateWishlistDto request)
    {
        try
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(new { message = "Vui lòng đăng nhập" });

            if (request.ProductId <= 0)
                return BadRequest(new { message = "Sản phẩm không hợp lệ" });

            var result = await _wishlistService.AddToWishlistAsync(userId, request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding product {ProductId} to wishlist", request.ProductId);
            return BadRequest(new { message = "Lỗi khi thêm vào danh sách yêu thích", error = ex.Message });
        }
    }

    [HttpDelete("product/{productId:int}")]
    public async Task<ActionResult> RemoveFromWishlist(int productId)
    {
        try
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(new { message = "Vui lòng đăng nhập" });

            var removed = await _wishlistService.RemoveFromWishlistAsync(userId, productId);
            if (!removed)
                return NotFound(new { message = "Sản phẩm không có trong danh sách yêu thích" });

            return Ok(new { message = "Đã xóa sản phẩm khỏi danh sách yêu thích" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing product {ProductId} from wishlist", productId);
            return BadRequest(new { message = "Lỗi khi xóa sản phẩm khỏi danh sách yêu thích" });
        }
    }

    [HttpPut("{wishlistId:int}")]
    public async Task<ActionResult<WishlistDto>> UpdateWishlistItem(int wishlistId, [FromBody] UpdateWishlistDto request)
    {
        try
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(new { message = "Vui lòng đăng nhập" });

            var updated = await _wishlistService.UpdateWishlistItemAsync(userId, wishlistId, request);
            if (updated == null)
                return NotFound(new { message = "Không tìm thấy mục wishlist" });

            return Ok(updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating wishlist item {WishlistId}", wishlistId);
            return BadRequest(new { message = "Lỗi khi cập nhật mục wishlist", error = ex.Message });
        }
    }

    [HttpGet("price-drops")]
    public async Task<ActionResult<IEnumerable<WishlistDto>>> GetPriceDrops()
    {
        try
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(new { message = "Vui lòng đăng nhập" });

            var result = await _wishlistService.GetPriceChangesAsync(userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting wishlist price drops");
            return BadRequest(new { message = "Lỗi khi lấy danh sách giảm giá" });
        }
    }

    [HttpGet("count")]
    public async Task<ActionResult<int>> GetWishlistCount()
    {
        try
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(new { message = "Vui lòng đăng nhập" });

            var count = await _wishlistService.GetWishlistCountAsync(userId);
            return Ok(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting wishlist count");
            return BadRequest(new { message = "Lỗi khi lấy số lượng wishlist" });
        }
    }

    [HttpGet("check/{productId:int}")]
    public async Task<ActionResult<bool>> IsInWishlist(int productId)
    {
        try
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(new { message = "Vui lòng đăng nhập" });

            var exists = await _wishlistService.IsProductInWishlistAsync(userId, productId);
            return Ok(exists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking wishlist for product {ProductId}", productId);
            return BadRequest(new { message = "Lỗi khi kiểm tra wishlist" });
        }
    }

    [HttpPost("product/{productId:int}/purchased")]
    public async Task<ActionResult> MarkAsPurchased(int productId)
    {
        try
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(new { message = "Vui lòng đăng nhập" });

            var updated = await _wishlistService.MarkAsPurchasedAsync(userId, productId);
            if (!updated)
                return NotFound(new { message = "Không tìm thấy sản phẩm trong wishlist" });

            return Ok(new { message = "Đã đánh dấu đã mua" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking product {ProductId} as purchased", productId);
            return BadRequest(new { message = "Lỗi khi cập nhật trạng thái đã mua" });
        }
    }

    private string? GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.FindFirst("sub")?.Value;
    }
}
