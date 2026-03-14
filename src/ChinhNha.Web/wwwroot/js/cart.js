function addToCart(productId, quantity = 1, variantId = null) {
    const payload = { productId: productId, quantity: quantity };
    if (variantId) {
        payload.variantId = variantId;
    }

    $.post('/Cart/AddToCart', payload, function (response) {
        if (response.success) {
            // Cập nhật số lượng trên icon giỏ hàng header
            $('.cart-count').text(response.totalItems);
            
            // Hiển thị toast thông báo
            if (typeof showToast === 'function') {
                showToast("Thành công", "Đã thêm sản phẩm vào giỏ hàng!");
            }
        } else if (response.requiresVariantSelection && response.redirectUrl) {
            window.location.href = response.redirectUrl;
        } else {
            if (typeof showToast === 'function') {
                showToast("Lỗi", "Không thể thêm vào giỏ: " + response.message, "error");
            }
        }
    }).fail(function () {
        if (typeof showToast === 'function') {
            showToast("Lỗi", "Có lỗi xảy ra khi kết nối đến máy chủ.", "error");
        }
    });
}

function addToCartAndGoCart(productId, quantity = 1, variantId = null) {
    const payload = { productId: productId, quantity: quantity };
    if (variantId) {
        payload.variantId = variantId;
    }

    $.post('/Cart/AddToCart', payload, function (response) {
        if (response.success) {
            $('.cart-count').text(response.totalItems);
            window.location.href = '/Cart';
        } else if (response.requiresVariantSelection && response.redirectUrl) {
            window.location.href = response.redirectUrl;
        } else {
            if (typeof showToast === 'function') {
                showToast("Lỗi", "Không thể thêm vào giỏ: " + response.message, "error");
            }
        }
    }).fail(function () {
        if (typeof showToast === 'function') {
            showToast("Lỗi", "Có lỗi xảy ra khi kết nối đến máy chủ.", "error");
        }
    });
}

function refreshCartCount() {
    $.get('/Cart/GetCartCount', function (response) {
        if (response && response.success) {
            $('.cart-count').text(response.totalItems);
        }
    });
}

$(function () {
    refreshCartCount();
});
