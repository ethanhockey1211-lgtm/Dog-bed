// Navbar shadow on scroll
const navbar = document.querySelector('.navbar');
if (navbar) {
    window.addEventListener('scroll', () => {
        navbar.classList.toggle('scrolled', window.scrollY > 10);
    }, { passive: true });
}

// Smooth scroll for anchor links
document.querySelectorAll('a[href^="#"]').forEach(link => {
    link.addEventListener('click', e => {
        const target = document.querySelector(link.getAttribute('href'));
        if (target) {
            e.preventDefault();
            const offset = 90;
            const top = target.getBoundingClientRect().top + window.scrollY - offset;
            window.scrollTo({ top, behavior: 'smooth' });
        }
    });
});

// Fade-up on scroll
const fadeObserver = new IntersectionObserver((entries) => {
    entries.forEach((entry, i) => {
        if (entry.isIntersecting) {
            setTimeout(() => entry.target.classList.add('visible'), i * 60);
            fadeObserver.unobserve(entry.target);
        }
    });
}, { threshold: 0.08 });

document.querySelectorAll('.feature-card, .review-card, .size-table-wrapper, .xsell-card, .guarantee-band')
    .forEach(el => {
        el.classList.add('fade-up');
        fadeObserver.observe(el);
    });

// ── Sale countdown (announcement bar) ─────────────────────────
(function () {
    const pill = document.getElementById('saleCountdown');
    const val = document.getElementById('saleCountdownVal');
    if (!pill || !val) return;
    const end = new Date(pill.dataset.end);
    if (isNaN(end)) { pill.style.display = 'none'; return; }

    function pad(n) { return String(n).padStart(2, '0'); }
    function tick() {
        const diff = end - Date.now();
        if (diff <= 0) { pill.style.display = 'none'; return; }
        const d = Math.floor(diff / 86400000);
        const h = Math.floor((diff % 86400000) / 3600000);
        const m = Math.floor((diff % 3600000) / 60000);
        const s = Math.floor((diff % 60000) / 1000);
        val.textContent = (d > 0 ? d + 'd ' : '') + pad(h) + ':' + pad(m) + ':' + pad(s);
    }
    tick();
    setInterval(tick, 1000);
})();

// ── Copy to clipboard helper (used by admin + promo popup) ────
function copyText(text, el) {
    const done = () => {
        const toast = document.getElementById('copiedToast');
        if (toast) {
            toast.classList.add('show');
            clearTimeout(toast._t);
            toast._t = setTimeout(() => toast.classList.remove('show'), 1600);
        }
        if (el) {
            el.classList.add('copied');
            clearTimeout(el._t);
            el._t = setTimeout(() => el.classList.remove('copied'), 1800);
        }
    };
    if (navigator.clipboard && window.isSecureContext) {
        navigator.clipboard.writeText(text).then(done).catch(() => fallbackCopy(text, done));
    } else {
        fallbackCopy(text, done);
    }
}
function fallbackCopy(text, done) {
    const ta = document.createElement('textarea');
    ta.value = text;
    ta.style.position = 'fixed';
    ta.style.opacity = '0';
    document.body.appendChild(ta);
    ta.select();
    try { document.execCommand('copy'); done(); } catch (e) { /* ignore */ }
    document.body.removeChild(ta);
}

// ── First-order offer popup (once per session) ────────────────
(function () {
    const overlay = document.getElementById('promoOverlay');
    if (!overlay) return;

    const path = window.location.pathname.toLowerCase();
    if (path.startsWith('/orders') || path.startsWith('/success') || path.startsWith('/checkout')) return;
    if (sessionStorage.getItem('ph_offer_seen')) return;

    let shown = false;
    function show() {
        if (shown) return;
        shown = true;
        sessionStorage.setItem('ph_offer_seen', '1');
        overlay.classList.add('show');
    }
    function hide() { overlay.classList.remove('show'); }

    // Exit intent on desktop, timed fallback everywhere
    document.addEventListener('mouseout', e => {
        if (!e.relatedTarget && e.clientY < 10) show();
    });
    setTimeout(show, 45000);

    document.getElementById('promoClose')?.addEventListener('click', hide);
    document.getElementById('promoDismiss')?.addEventListener('click', hide);
    overlay.addEventListener('click', e => { if (e.target === overlay) hide(); });

    const codeBox = document.getElementById('promoCodeBox');
    codeBox?.addEventListener('click', () => copyText(codeBox.textContent.trim(), codeBox));
})();
