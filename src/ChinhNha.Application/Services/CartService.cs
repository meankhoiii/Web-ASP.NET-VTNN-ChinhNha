using AutoMapper;
using ChinhNha.Application.DTOs.Orders;
using ChinhNha.Application.Interfaces;
using ChinhNha.Domain.Entities;
using ChinhNha.Domain.Interfaces;

namespace ChinhNha.Application.Services;

public class CartService : ICartService
{
    private readonly ICartRepository _cartRepository;
    private readonly IRepository<CartItem> _cartItemRepository;
    private readonly IProductRepository _productRepository;
    private readonly IMapper _mapper;

    public CartService(ICartRepository cartRepository, IRepository<CartItem> cartItemRepository, IProductRepository productRepository, IMapper mapper)
    {
        _cartRepository = cartRepository;
        _cartItemRepository = cartItemRepository;
        _productRepository = productRepository;
        _mapper = mapper;
    }

    public async Task<CartDto> AddItemToCartAsync(string? userId, string sessionId, int productId, int quantity, int? variantId = null)
    {
        var cart = await _cartRepository.GetCartWithItemsAsync(userId, sessionId);
        if (cart == null)
        {
            cart = new Cart { UserId = userId, SessionId = sessionId };
            await _cartRepository.AddAsync(cart);
        }

        var product = await _productRepository.GetProductWithDetailsByIdAsync(productId);
        if (product == null) throw new ArgumentException("Product not found");

        var existingItem = cart.CartItems.FirstOrDefault(i => i.ProductId == productId && i.ProductVariantId == variantId);
        if (existingItem != null)
        {
            existingItem.Quantity += quantity;
        }
        else
        {
            decimal price = product.SalePrice ?? product.BasePrice;
            if (variantId.HasValue)
            {
                var variant = product.Variants.FirstOrDefault(v => v.Id == variantId.Value);
                if (variant != null) price = variant.SalePrice ?? variant.Price;
            }

            cart.CartItems.Add(new CartItem
            {
                ProductId = productId,
                ProductVariantId = variantId,
                Quantity = quantity,
                UnitPrice = price
            });
        }

        cart.UpdatedAt = DateTime.UtcNow;
        await _cartRepository.UpdateAsync(cart);
        return _mapper.Map<CartDto>(cart);
    }

    public async Task<bool> ClearCartAsync(int cartId)
    {
        var cart = await _cartRepository.GetByIdAsync(cartId);
        if (cart == null) return false;
        await _cartRepository.DeleteAsync(cart);
        return true;
    }

    public async Task<CartDto> GetCartAsync(string? userId, string sessionId)
    {
        var cart = await _cartRepository.GetCartWithItemsAsync(userId, sessionId);
        if (cart == null)
        {
            cart = new Cart { UserId = userId, SessionId = sessionId };
            await _cartRepository.AddAsync(cart);
        }
        return _mapper.Map<CartDto>(cart);
    }

    public async Task<CartDto> MergeGuestCartToUserCartAsync(string sessionId, string userId)
    {
        var guestCart = await _cartRepository.GetCartWithItemsAsync(null, sessionId);
        var userCart = await _cartRepository.GetCartWithItemsAsync(userId, sessionId);

        if (guestCart == null || !guestCart.CartItems.Any())
            return _mapper.Map<CartDto>(userCart ?? new Cart { UserId = userId, SessionId = sessionId });

        if (userCart == null)
        {
            guestCart.UserId = userId;
            await _cartRepository.UpdateAsync(guestCart);
            return _mapper.Map<CartDto>(guestCart);
        }

        // Merge guest items into user cart
        foreach (var guestItem in guestCart.CartItems)
        {
            var existingItem = userCart.CartItems.FirstOrDefault(
                i => i.ProductId == guestItem.ProductId && i.ProductVariantId == guestItem.ProductVariantId);

            if (existingItem != null)
            {
                existingItem.Quantity += guestItem.Quantity;
            }
            else
            {
                userCart.CartItems.Add(new CartItem
                {
                    ProductId = guestItem.ProductId,
                    ProductVariantId = guestItem.ProductVariantId,
                    Quantity = guestItem.Quantity,
                    UnitPrice = guestItem.UnitPrice
                });
            }
        }

        userCart.UpdatedAt = DateTime.UtcNow;
        await _cartRepository.UpdateAsync(userCart);
        await _cartRepository.DeleteAsync(guestCart);

        return _mapper.Map<CartDto>(userCart);
    }

    public async Task<bool> RemoveItemFromCartAsync(int cartItemId)
    {
        var cartItem = await _cartItemRepository.GetByIdAsync(cartItemId);
        if (cartItem == null)
        {
            return false;
        }

        var cart = await _cartRepository.GetByIdAsync(cartItem.CartId);
        if (cart != null)
        {
            cart.UpdatedAt = DateTime.UtcNow;
            await _cartRepository.UpdateAsync(cart);
        }

        await _cartItemRepository.DeleteAsync(cartItem);
        return true;
    }

    public async Task<CartDto> UpdateCartItemQuantityAsync(int cartItemId, int quantity)
    {
        var cartItem = await _cartItemRepository.GetByIdAsync(cartItemId)
            ?? throw new ArgumentException("Cart item not found", nameof(cartItemId));

        if (quantity <= 0)
        {
            var cartId = cartItem.CartId;
            await _cartItemRepository.DeleteAsync(cartItem);

            var updatedCartAfterDelete = await _cartRepository.GetCartWithItemsByIdAsync(cartId)
                ?? throw new InvalidOperationException("Cart not found.");

            updatedCartAfterDelete.UpdatedAt = DateTime.UtcNow;
            await _cartRepository.UpdateAsync(updatedCartAfterDelete);
            return _mapper.Map<CartDto>(updatedCartAfterDelete);
        }

        cartItem.Quantity = quantity;
        await _cartItemRepository.UpdateAsync(cartItem);

        var updatedCart = await _cartRepository.GetCartWithItemsByIdAsync(cartItem.CartId)
            ?? throw new InvalidOperationException("Cart not found.");

        updatedCart.UpdatedAt = DateTime.UtcNow;
        await _cartRepository.UpdateAsync(updatedCart);
        return _mapper.Map<CartDto>(updatedCart);
    }
}
