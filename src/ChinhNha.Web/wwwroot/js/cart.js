function addToCart(productId, quantity = 1) {
    $.post('/Cart/AddToCart', { productId: productId, quantity: quantity }, function (response) {
        if (response.success) {
            // Cập nhật số lượng trên icon giỏ hàng header
            $('.cart-count').text(response.totalItems);
            
            // Hiển thị toast thông báo
            if (typeof showToast === 'function') {
                showToast("Thành công", "Đã thêm sản phẩm vào giỏ hàng!");
            }
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
