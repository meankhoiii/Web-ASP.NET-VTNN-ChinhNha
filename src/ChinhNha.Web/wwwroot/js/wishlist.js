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
            showWishlistToast('Yeu cau dang nhap', 'Vui long dang nhap de su dung wishlist.', 'error');
            window.location.href = '/Account/Login';
            return { success: false, unauthorized: true };
        }

        const data = await parseJsonSafe(res);
        if (!res.ok) {
            const msg = data && data.message ? data.message : 'Khong the them vao wishlist.';
            showWishlistToast('Loi', msg, 'error');
            return { success: false };
        }

        await updateWishlistCount();
        showWishlistToast('Thanh cong', 'Da them san pham vao danh sach yeu thich.');
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
            const msg = data && data.message ? data.message : 'Khong the xoa khoi wishlist.';
            showWishlistToast('Loi', msg, 'error');
            return { success: false };
        }

        await updateWishlistCount();
        showWishlistToast('Thanh cong', 'Da xoa san pham khoi wishlist.');
        return { success: true };
    }

    window.updateWishlistCount = updateWishlistCount;
    window.addToWishlist = addToWishlist;
    window.removeFromWishlist = removeFromWishlist;

    document.addEventListener('DOMContentLoaded', function () {
        updateWishlistCount();
    });
})();
