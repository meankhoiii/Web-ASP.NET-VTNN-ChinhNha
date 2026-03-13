(function () {
    function showWishlistToast(title, message, type) {
        if (typeof showToast === "function") {
            showToast(title, message, type || "success");
            return;
        }
        alert(title + ": " + message);
    }

    async function parseJsonSafe(response) {
        try {
            return await response.json();
        } catch {
            return null;
        }
    }

    function setWishlistCount(count) {
        document.querySelectorAll('.wishlist-count').forEach(function (el) {
            el.textContent = String(count);
        });
    }

    async function updateWishlistCount() {
        if (!document.querySelector('.wishlist-count')) {
            return;
        }

        try {
            const res = await fetch('/api/Wishlist/count', { credentials: 'include' });
            if (res.status === 401) {
                setWishlistCount(0);
                return;
            }
            if (!res.ok) {
                return;
            }
            const data = await parseJsonSafe(res);
            const count = typeof data === 'number' ? data : 0;
            setWishlistCount(count);
        } catch {
            // Silent fail to avoid breaking page rendering.
        }
    }

    async function addToWishlist(productId, wishlistName, notes, priority) {
        const payload = {
            productId: productId,
            wishlistName: wishlistName || null,
            notes: notes || null,
            priority: priority || 3
        };

        const res = await fetch('/api/Wishlist', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify(payload)
        });

        if (res.status === 401) {
            showWishlistToast('Yêu cầu đăng nhập', 'Vui lòng đăng nhập để sử dụng wishlist.', 'error');
            window.location.href = '/Account/Login';
            return { success: false, unauthorized: true };
        }

        const data = await parseJsonSafe(res);
        if (!res.ok) {
            const msg = data && data.message ? data.message : 'Không thể thêm vào wishlist.';
            showWishlistToast('Lỗi', msg, 'error');
            return { success: false };
        }

        await updateWishlistCount();
        showWishlistToast('Thành công', 'Đã thêm sản phẩm vào danh sách yêu thích.');
        return { success: true, data: data };
    }

    async function removeFromWishlist(productId) {
        const res = await fetch('/api/Wishlist/product/' + productId, {
            method: 'DELETE',
            credentials: 'include'
        });

        if (res.status === 401) {
            window.location.href = '/Account/Login';
            return { success: false, unauthorized: true };
        }

        const data = await parseJsonSafe(res);
        if (!res.ok) {
            const msg = data && data.message ? data.message : 'Không thể xóa khỏi wishlist.';
            showWishlistToast('Lỗi', msg, 'error');
            return { success: false };
        }

        await updateWishlistCount();
        showWishlistToast('Thành công', 'Đã xóa sản phẩm khỏi wishlist.');
        return { success: true };
    }

    window.updateWishlistCount = updateWishlistCount;
    window.addToWishlist = addToWishlist;
    window.removeFromWishlist = removeFromWishlist;

    document.addEventListener('DOMContentLoaded', function () {
        updateWishlistCount();
    });
})();
