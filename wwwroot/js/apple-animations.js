/* ═══════════════════════════════════════════════════════════════════════
   iOStore — Apple Animations v1.0
   IntersectionObserver fade-in, parallax hero, navbar scroll, gallery
   ═══════════════════════════════════════════════════════════════════════ */

(function () {
  'use strict';

  /* ── 1. NAVBAR — sombra en scroll ─────────────────────────────────────── */
  (function initNavbarScroll() {
    const navbar = document.querySelector('.navbar');
    if (!navbar) return;

    let ticking = false;
    window.addEventListener('scroll', () => {
      if (!ticking) {
        requestAnimationFrame(() => {
          if (window.scrollY > 4) {
            navbar.classList.add('scrolled');
          } else {
            navbar.classList.remove('scrolled');
          }
          ticking = false;
        });
        ticking = true;
      }
    }, { passive: true });
  })();

  /* ── 2. FADE-IN SCROLL (IntersectionObserver) ─────────────────────────── */
  (function initFadeInObserver() {
    const elements = document.querySelectorAll('.fade-in-up');
    if (!elements.length) return;

    const observer = new IntersectionObserver((entries) => {
      entries.forEach(entry => {
        if (entry.isIntersecting) {
          // Stagger delay basado en el índice dentro de su padre
          const siblings = Array.from(entry.target.parentElement?.children || []);
          const idx = siblings.indexOf(entry.target);
          entry.target.style.transitionDelay = (idx * 0.08) + 's';
          entry.target.classList.add('is-visible');
          observer.unobserve(entry.target);
        }
      });
    }, {
      threshold: 0.12,
      rootMargin: '0px 0px -40px 0px'
    });

    elements.forEach(el => observer.observe(el));
  })();

  /* ── 3. PARALLAX HERO ─────────────────────────────────────────────────── */
  (function initHeroParallax() {
    const hero = document.querySelector('.hero-apple');
    const inner = document.querySelector('.hero-parallax-inner');
    if (!hero || !inner) return;
    if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) return;

    let ticking = false;
    window.addEventListener('scroll', () => {
      if (!ticking) {
        requestAnimationFrame(() => {
          const offset = window.scrollY;
          const heroH  = hero.offsetHeight;
          if (offset < heroH) {
            inner.style.transform = `translateY(${offset * 0.28}px)`;
          }
          ticking = false;
        });
        ticking = true;
      }
    }, { passive: true });
  })();

  /* ── 4. GALERÍA DE IMÁGENES (detalle producto) ────────────────────────── */
  (function initProductGallery() {
    const mainImg   = document.getElementById('galleryMain');
    const thumbBtns = document.querySelectorAll('.gallery-thumb');
    if (!mainImg || !thumbBtns.length) return;

    thumbBtns.forEach(btn => {
      btn.addEventListener('click', function () {
        const src = this.dataset.src;
        if (!src) return;

        // Fade out → change → fade in
        mainImg.style.opacity = '0';
        mainImg.style.transform = 'scale(0.97)';

        setTimeout(() => {
          mainImg.src = src;
          mainImg.alt = this.dataset.alt || '';
          mainImg.style.opacity = '1';
          mainImg.style.transform = 'scale(1)';
        }, 200);

        // Actualizar thumb activo
        thumbBtns.forEach(t => t.classList.remove('active'));
        this.classList.add('active');
      });
    });

    // Estilo para la transición
    mainImg.style.transition = 'opacity 0.2s ease, transform 0.25s ease';
  })();

  /* ── 5. SELECTOR DE ALMACENAMIENTO ───────────────────────────────────── */
  (function initStorageSelector() {
    const btns = document.querySelectorAll('.storage-btn');
    btns.forEach(btn => {
      btn.addEventListener('click', function () {
        btns.forEach(b => b.classList.remove('active'));
        this.classList.add('active');
      });
    });
  })();

  /* ── 6. SPECS ACORDEÓN ────────────────────────────────────────────────── */
  (function initSpecsAccordion() {
    const triggers = document.querySelectorAll('.specs-accordion-btn');
    triggers.forEach(btn => {
      btn.addEventListener('click', function () {
        const body = this.nextElementSibling;
        const isOpen = body.classList.contains('open');

        // Cerrar todos
        document.querySelectorAll('.specs-accordion-body').forEach(b => {
          b.classList.remove('open');
        });
        document.querySelectorAll('.specs-accordion-btn').forEach(b => {
          b.setAttribute('aria-expanded', 'false');
        });

        // Abrir este si estaba cerrado
        if (!isOpen) {
          body.classList.add('open');
          this.setAttribute('aria-expanded', 'true');
        }
      });
    });
  })();

  /* ── 7. FILTROS SIDEBAR — toggle mobile ──────────────────────────────── */
  (function initFilterToggle() {
    const toggleBtn = document.getElementById('btnFilterToggle');
    const sidebar   = document.querySelector('.filter-sidebar');
    if (!toggleBtn || !sidebar) return;

    toggleBtn.addEventListener('click', () => {
      sidebar.classList.toggle('open');
      const isOpen = sidebar.classList.contains('open');
      toggleBtn.innerHTML = isOpen
        ? '<i class="bi bi-x-lg me-1"></i>Cerrar filtros'
        : '<i class="bi bi-sliders me-1"></i>Filtros';
    });
  })();

  /* ── 8. FILTROS SIDEBAR — acordeón por sección ────────────────────────── */
  (function initFilterAccordion() {
    const labels = document.querySelectorAll('.filter-section-label[data-accordion]');
    labels.forEach(label => {
      label.addEventListener('click', function () {
        const targetId = this.dataset.accordion;
        const body = document.getElementById(targetId);
        if (!body) return;

        const isOpen = body.style.display !== 'none' && body.style.display !== '';
        body.style.display = isOpen ? 'none' : 'flex';
        this.setAttribute('aria-expanded', isOpen ? 'false' : 'true');
      });
    });
  })();

  /* ── 9. ANIMACIÓN BOTONES active state ────────────────────────────────── */
  (function initButtonFeedback() {
    const btns = document.querySelectorAll(
      '.btn-apple-primary, .btn-buy-now, .btn-checkout-main, .btn-confirm-order, .btn-card-buy'
    );
    btns.forEach(btn => {
      btn.addEventListener('mousedown', function () {
        this.style.transform = 'scale(0.97)';
      });
      btn.addEventListener('mouseup', function () {
        this.style.transform = '';
      });
      btn.addEventListener('mouseleave', function () {
        this.style.transform = '';
      });
    });
  })();

  /* ── 10. SKELETON IMAGES — lazy load ─────────────────────────────────── */
  (function initImageSkeleton() {
    const imgs = document.querySelectorAll('img[data-src]');
    if (!imgs.length) return;

    const imgObserver = new IntersectionObserver((entries) => {
      entries.forEach(entry => {
        if (entry.isIntersecting) {
          const img = entry.target;
          const parent = img.closest('.card-img-wrap, .gallery-main');

          if (parent) parent.classList.add('skeleton');

          img.src = img.dataset.src;
          img.onload = () => {
            if (parent) parent.classList.remove('skeleton');
            img.style.opacity = '1';
          };
          imgObserver.unobserve(img);
        }
      });
    }, { rootMargin: '100px' });

    imgs.forEach(img => {
      img.style.opacity = '0';
      img.style.transition = 'opacity 0.3s ease';
      imgObserver.observe(img);
    });
  })();

  /* ── 11. TRANSICIÓN DE PÁGINA — fade-out suave ────────────────────────── */
  (function initPageTransition() {
    if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) return;

    document.addEventListener('click', function (e) {
      const link = e.target.closest('a[href]');
      if (!link) return;

      const href = link.getAttribute('href');
      // Solo links internos, no anclas ni javascript: ni target=_blank
      if (!href || href.startsWith('#') || href.startsWith('javascript') ||
          link.target === '_blank' || link.hostname !== location.hostname) return;
      // No aplicar en formularios y acciones AJAX
      if (link.closest('form')) return;

      e.preventDefault();
      document.body.style.opacity = '0';
      document.body.style.transition = 'opacity 0.18s ease';
      setTimeout(() => { window.location.href = href; }, 180);
    });

    // Restaurar en pageshow (navegación con back/forward)
    window.addEventListener('pageshow', () => {
      document.body.style.opacity = '1';
    });
  })();

  /* ── 12. SORT SELECT — auto-submit en catálogo ────────────────────────── */
  (function initSortSelect() {
    const sortSelect = document.getElementById('sortSelect');
    if (!sortSelect) return;
    sortSelect.addEventListener('change', function () {
      this.closest('form')?.submit();
    });
  })();

})();
