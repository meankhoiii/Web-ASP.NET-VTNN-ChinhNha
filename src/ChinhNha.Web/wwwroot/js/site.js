// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

(function () {
	function ensureToastHost() {
		var host = document.getElementById('appToastHost');
		if (host) {
			return host;
		}

		host = document.createElement('div');
		host.id = 'appToastHost';
		host.style.position = 'fixed';
		host.style.top = '20px';
		host.style.right = '20px';
		host.style.zIndex = '3000';
		host.style.display = 'flex';
		host.style.flexDirection = 'column';
		host.style.gap = '10px';
		host.style.maxWidth = '90vw';
		document.body.appendChild(host);
		return host;
	}

	function getToastColors(type) {
		switch ((type || 'success').toLowerCase()) {
			case 'error':
				return { bg: '#fdecea', border: '#ef9a9a', title: '#b71c1c', text: '#7f0000' };
			case 'warning':
				return { bg: '#fff8e1', border: '#ffcc80', title: '#ef6c00', text: '#5d4037' };
			default:
				return { bg: '#e8f5e9', border: '#a5d6a7', title: '#1b5e20', text: '#2e7d32' };
		}
	}

	window.showToast = function (title, message, type) {
		try {
			var host = ensureToastHost();
			var colors = getToastColors(type);

			var toast = document.createElement('div');
			toast.style.background = colors.bg;
			toast.style.border = '2px solid ' + colors.border;
			toast.style.borderRadius = '10px';
			toast.style.padding = '10px 12px';
			toast.style.boxShadow = '0 8px 20px rgba(0,0,0,0.12)';
			toast.style.minWidth = '240px';
			toast.style.maxWidth = '360px';
			toast.style.opacity = '0';
			toast.style.transition = 'opacity .2s ease';

			toast.innerHTML = ''
				+ '<div style="display:flex;justify-content:space-between;align-items:start;gap:10px;">'
				+ '  <div>'
				+ '    <div style="font-weight:800;color:' + colors.title + ';">' + (title || 'Thong bao') + '</div>'
				+ '    <div style="font-size:.92rem;color:' + colors.text + ';line-height:1.35;">' + (message || '') + '</div>'
				+ '  </div>'
				+ '  <button type="button" aria-label="close" style="border:none;background:transparent;color:' + colors.title + ';font-size:1.1rem;line-height:1;cursor:pointer;">x</button>'
				+ '</div>';

			var closeBtn = toast.querySelector('button');
			var removeToast = function () {
				toast.style.opacity = '0';
				setTimeout(function () {
					if (toast.parentElement) {
						toast.parentElement.removeChild(toast);
					}
				}, 200);
			};

			closeBtn.addEventListener('click', removeToast);
			host.appendChild(toast);
			requestAnimationFrame(function () {
				toast.style.opacity = '1';
			});

			setTimeout(removeToast, 2600);
		} catch {
			alert((title || 'Thong bao') + ': ' + (message || ''));
		}
	};
})();
