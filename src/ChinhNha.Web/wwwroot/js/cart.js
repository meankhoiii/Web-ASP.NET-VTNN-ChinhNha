function addToCart(productId, quantity = 1) {
    $.post('/Cart/AddToCart', { productId: productId, quantity: quantity }, function (response) {
        if (response.success) {
            // Hiển thị toast hoặc thay đổi icon (tạm thời alert)
            
            // Cập nhật số lượng trên icon giỏ hàng header
            $('.cart-badge').text(response.totalItems);
            
            // Hiển thị toast thông báo
            showToast("Thành công", "Đã thêm sản phẩm vào giỏ hàng!");
        } else {
            showToast("Lỗi", "Không thể thêm vào giỏ: " + response.message, "error");
        }
    }).fail(function () {
        showToast("Lỗi", "Có lỗi xảy ra khi kết nối đến máy chủ.", "error");
    });
}

function showToast(title, message, type = "success") {
    // Basic implementation of toast if Bootstrap toast is not set up
    // In a real app we would manipulate DOM to show a fixed toast
    alert(title + ": " + message);
}
