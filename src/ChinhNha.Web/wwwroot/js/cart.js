function addToCart(productId, quantity = 1) {
    $.post('/Cart/AddToCart', { productId: productId, quantity: quantity }, function (response) {
        if (response.success) {
            // Cập nhật số lượng trên icon giỏ hàng header
            $('.cart-count').text(response.totalItems);
            
            // Hiển thị toast thông báo
            if (typeof showToast === 'function') {
                showToast("Thanh cong", "Da them san pham vao gio hang!");
            }
        } else {
            if (typeof showToast === 'function') {
                showToast("Loi", "Khong the them vao gio: " + response.message, "error");
            }
        }
    }).fail(function () {
        if (typeof showToast === 'function') {
            showToast("Loi", "Co loi xay ra khi ket noi den may chu.", "error");
        }
    });
}
