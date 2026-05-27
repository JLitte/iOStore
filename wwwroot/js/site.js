/* iOStore — site.js */

// ── Toast API ─────────────────────────────────────────────────────────────
// window.toast.success("msg") / .error() / .warning() / .info()
// showToast(msg, type) sigue funcionando por compatibilidad

window.toast = (function () {
    function getContainer() {
        let c = document.getElementById('toastContainer');
        if (!c) {
            c = document.createElement('div');
            c.id = 'toastContainer';
            document.body.appendChild(c);
        }
        return c;
    }

    const icons = {
        success: '<i class="bi bi-check-circle-fill t-icon" style="color:var(--success)"></i>',
        error:   '<i class="bi bi-x-circle-fill t-icon" style="color:var(--danger)"></i>',
        warning: '<i class="bi bi-exclamation-triangle-fill t-icon" style="color:var(--warning)"></i>',
        info:    '<i class="bi bi-info-circle-fill t-icon" style="color:var(--accent)"></i>'
    };

    function show(msg, type = 'info') {
        const container = getContainer();
        const el = document.createElement('div');
        el.className = `toast-custom t-${type}`;
        el.innerHTML = `
            ${icons[type] || icons.info}
            <span>${msg}</span>
            <button class="t-close" aria-label="Cerrar">✕</button>
        `;

        const close = () => {
            el.style.animation = 'fadeOut .25s ease forwards';
            setTimeout(() => el.remove(), 250);
        };

        el.querySelector('.t-close').addEventListener('click', close);
        el.addEventListener('click', close);

        container.appendChild(el);
        setTimeout(close, 3500);
    }

    return {
        success: (m) => show(m, 'success'),
        error:   (m) => show(m, 'error'),
        warning: (m) => show(m, 'warning'),
        info:    (m) => show(m, 'info'),
        show
    };
})();

// Alias de compatibilidad con el código existente
function showToast(msg, type = 'success') {
    // Mapear tipos de Bootstrap a los nuevos
    const map = { danger: 'error', success: 'success', warning: 'warning', info: 'info' };
    window.toast.show(msg, map[type] || type);
}

// ── Modal Confirm — reemplaza confirm() nativo ────────────────────────────
// Uso: const ok = await confirmDialog("¿Eliminar producto?", "Esta acción no se puede deshacer.")
function confirmDialog(title = '¿Estás seguro?', description = '', btnText = 'Confirmar') {
    return new Promise(resolve => {
        const overlay = document.createElement('div');
        overlay.className = 'confirm-overlay';
        overlay.innerHTML = `
            <div class="confirm-card animate-scale-in">
                <div class="confirm-icon"><i class="bi bi-exclamation-triangle-fill"></i></div>
                <div class="confirm-title">${title}</div>
                <div class="confirm-desc">${description}</div>
                <div class="d-flex gap-2 justify-content-center">
                    <button class="btn btn-secondary btn-confirm-cancel">Cancelar</button>
                    <button class="btn btn-danger btn-confirm-ok">${btnText}</button>
                </div>
            </div>
        `;

        const close = (result) => {
            overlay.style.animation = 'fadeOut .15s ease forwards';
            setTimeout(() => { overlay.remove(); resolve(result); }, 150);
        };

        overlay.querySelector('.btn-confirm-cancel').addEventListener('click', () => close(false));
        overlay.querySelector('.btn-confirm-ok').addEventListener('click', () => close(true));
        overlay.addEventListener('click', e => { if (e.target === overlay) close(false); });

        document.addEventListener('keydown', function esc(e) {
            if (e.key === 'Escape') { close(false); document.removeEventListener('keydown', esc); }
        });

        document.body.appendChild(overlay);
        overlay.querySelector('.btn-confirm-ok').focus();
    });
}

// ── Auto-dismiss alertas ──────────────────────────────────────────────────
document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('.alert-dismissible').forEach(function (alert) {
        setTimeout(() => {
            try {
                const bsAlert = bootstrap.Alert.getOrCreateInstance(alert);
                bsAlert.close();
            } catch (e) {
                alert.style.animation = 'fadeOut .3s ease forwards';
                setTimeout(() => alert.remove(), 300);
            }
        }, 5000);
    });
});

// ── Función global para agregar al carrito ────────────────────────────────
function agregarAlCarrito(productoId, cantidad = 1, callback) {
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
    fetch('/Carrito/AgregarAlCarrito', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
            'RequestVerificationToken': token
        },
        body: `productoId=${productoId}&cantidad=${cantidad}`
    })
        .then(r => r.json())
        .then(data => {
            if (data.success) {
                const badge = document.getElementById('carritoCount');
                if (badge) badge.textContent = data.totalItems;
                window.toast.success(data.message || '¡Producto agregado al carrito!');
                if (typeof callback === 'function') callback(data);
            } else {
                window.toast.error(data.message || 'Error al agregar al carrito.');
            }
        })
        .catch(() => window.toast.error('Error de conexión.'));
}

// ── Loading state en botones de submit ───────────────────────────────────
document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('form').forEach(form => {
        form.addEventListener('submit', function () {
            const submitBtn = this.querySelector('[type="submit"]');
            if (submitBtn && !submitBtn.classList.contains('no-loading')) {
                const originalText = submitBtn.innerHTML;
                submitBtn.classList.add('btn-loading');
                submitBtn.disabled = true;
                // Re-habilitar tras 8s por si falla la navegación
                setTimeout(() => {
                    submitBtn.classList.remove('btn-loading');
                    submitBtn.disabled = false;
                    submitBtn.innerHTML = originalText;
                }, 8000);
            }
        });
    });
});

// ── Búsqueda sensitiva en navbar ──────────────────────────────────────────
document.addEventListener('DOMContentLoaded', function () {
    const input = document.getElementById('navSearchInput');
    const box = document.getElementById('navSearchSuggestions');
    if (!input || !box) return;

    let timer;

    input.addEventListener('input', function () {
        clearTimeout(timer);
        const q = this.value.trim();
        if (q.length < 2) { box.style.display = 'none'; return; }

        timer = setTimeout(() => {
            fetch(`/Producto/BuscarJson?q=${encodeURIComponent(q)}`)
                .then(r => r.json())
                .then(items => {
                    if (!items.length) { box.style.display = 'none'; return; }
                    box.innerHTML = items.map(p =>
                        `<a href="/Producto/Details/${p.id}">
                            <strong>${p.modelo}</strong>
                            <span class="suggestion-price">$${p.precio.toLocaleString('es-AR')}</span>
                            <span class="suggestion-type">${p.tipoProducto}</span>
                         </a>`
                    ).join('');
                    box.style.display = 'block';
                    // Posicionar bajo el input
                    const rect = input.getBoundingClientRect();
                    box.style.top  = (rect.bottom + 6) + 'px';
                    box.style.left = rect.left + 'px';
                    box.style.width = Math.max(rect.width, 280) + 'px';
                })
                .catch(() => { box.style.display = 'none'; });
        }, 280);
    });

    document.addEventListener('click', e => {
        if (!input.contains(e.target) && !box.contains(e.target))
            box.style.display = 'none';
    });

    document.addEventListener('keydown', e => {
        if (e.key === 'Escape') box.style.display = 'none';
    });
});

// ── OTP inputs — auto-focus + paste inteligente ───────────────────────────
document.addEventListener('DOMContentLoaded', function () {
    const otpInputs = document.querySelectorAll('.otp-input');
    if (!otpInputs.length) return;

    otpInputs.forEach((input, idx) => {
        input.addEventListener('input', function () {
            const val = this.value.replace(/\D/g, '');
            this.value = val.slice(-1); // un dígito
            if (val && idx < otpInputs.length - 1) {
                otpInputs[idx + 1].focus();
                otpInputs[idx + 1].select();
            }
            this.classList.toggle('filled', !!this.value);
            syncHiddenCode();
        });

        input.addEventListener('keydown', function (e) {
            if (e.key === 'Backspace' && !this.value && idx > 0) {
                otpInputs[idx - 1].focus();
                otpInputs[idx - 1].select();
            }
        });

        input.addEventListener('paste', function (e) {
            e.preventDefault();
            const pasted = (e.clipboardData || window.clipboardData)
                .getData('text').replace(/\D/g, '').slice(0, 6);
            pasted.split('').forEach((ch, i) => {
                if (otpInputs[i]) {
                    otpInputs[i].value = ch;
                    otpInputs[i].classList.add('filled');
                }
            });
            const next = Math.min(pasted.length, otpInputs.length - 1);
            otpInputs[next].focus();
            syncHiddenCode();
        });
    });

    function syncHiddenCode() {
        const hidden = document.getElementById('codigoHidden') || document.querySelector('input[name="Codigo"]');
        if (hidden) {
            hidden.value = Array.from(otpInputs).map(i => i.value).join('');
        }
    }
});

// ── Admin sidebar mobile toggle ───────────────────────────────────────────
document.addEventListener('DOMContentLoaded', function () {
    const toggle = document.getElementById('adminSidebarToggle');
    const sidebar = document.querySelector('.admin-sidebar');
    if (!toggle || !sidebar) return;

    toggle.addEventListener('click', () => sidebar.classList.toggle('open'));

    // Cerrar al hacer click fuera en mobile
    document.addEventListener('click', e => {
        if (window.innerWidth <= 768 &&
            sidebar.classList.contains('open') &&
            !sidebar.contains(e.target) &&
            e.target !== toggle) {
            sidebar.classList.remove('open');
        }
    });
});

// ── Vaciar carrito con modal confirm (reemplaza confirm() nativo) ─────────
document.addEventListener('DOMContentLoaded', function () {
    const btnVaciar = document.getElementById('btnVaciar');
    if (!btnVaciar) return;

    // Remover el onclick original si existe y reemplazar con modal
    btnVaciar.addEventListener('click', async function (e) {
        e.preventDefault();
        const ok = await confirmDialog(
            '¿Vaciar el carrito?',
            'Se eliminarán todos los productos del carrito.'
        );
        if (!ok) return;

        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
        fetch('/Carrito/VaciarCarrito', {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded', 'RequestVerificationToken': token },
            body: ''
        }).then(r => r.json()).then(data => { if (data.success) location.reload(); });
    });
});
