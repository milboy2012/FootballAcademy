(() => {
    

    const TYPE = {
        0: ['Объявление', 'primary', 'megaphone'],
        1: ['Новость', 'success', 'newspaper'],
        2: ['Праздник', 'warning text-dark', 'balloon'],
        3: ['Соревнование', 'danger', 'trophy']
    };
    const fmt = d => new Date(d).toLocaleDateString('ru-RU', { day: 'numeric', month: 'long', year: 'numeric' });
    const fmtDT = d => new Date(d).toLocaleString('ru-RU', { day: 'numeric', month: 'long', hour: '2-digit', minute: '2-digit' });
    const fmtFull = d => new Date(d).toLocaleString('ru-RU', { day: 'numeric', month: 'long', year: 'numeric', hour: '2-digit', minute: '2-digit' });
    const esc = s => (s ?? '').replace(/[&<>"]/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' }[c]));
    const md = s => esc(s).replace(/\*\*(.+?)\*\*/g, '<b>$1</b>').replace(/\n/g, '<br>');

    const cache = new Map();                // id -> post
    const viewModalEl = document.getElementById('postViewModal');
    const viewModal = viewModalEl ? new bootstrap.Modal(viewModalEl) : null;
    const $ = id => document.getElementById(id);

    function openPost(id) {
        const p = cache.get(id);
        if (!p || !viewModal) return;
        const [name, cls, icon] = TYPE[p.type];

        $('pvBadge').innerHTML = `<span class="badge bg-${cls}"><i class="bi bi-${icon}"></i> ${name}</span>` +
            (p.groupName ? ` <span class="badge bg-light text-dark">${esc(p.groupName)}</span>` : '') +
            (p.isPinned ? ' <span class="badge bg-light text-dark">📌 закреплено</span>' : '');
        $('pvTitle').textContent = p.title;

        const img = $('pvImage');
        img.classList.toggle('d-none', !p.imagePath);
        img.src = p.imagePath ?? '';

        $('pvEvent').innerHTML = p.eventDate
            ? `<i class="bi bi-calendar-event"></i> ${fmtDT(p.eventDate)}${p.location ? ` · <i class="bi bi-geo-alt"></i> ${esc(p.location)}` : ''}`
            : '';
        $('pvBody').innerHTML = esc(p.body).replace(/\*\*(.+?)\*\*/g, '<b>$1</b>'); // переносы даёт white-space:pre-wrap
        $('pvAuthor').textContent = p.authorName ?? '';
        $('pvDate').textContent = fmtFull(p.publishedAt);

        viewModal.show();
        history.replaceState(null, '', `#post-${id}`);
    }
    viewModalEl?.addEventListener('hidden.bs.modal', () => history.replaceState(null, '', location.pathname));

    async function load(type = '') {
        const posts = await (await fetch(`/api/posts/feed?type=${type}&take=30`)).json();
        posts.forEach(p => cache.set(String(p.id), p));

        document.getElementById('feed').innerHTML = posts.length ? posts.map(p => {
            const [name, cls, icon] = TYPE[p.type];
            const long = p.body.length > 400;
            return `<div class="card post-card ${p.isPinned ? 'border-primary' : ''}" data-id="${p.id}" role="button">
                ${p.imagePath ? `<img src="${p.imagePath}" class="card-img-top" style="max-height:320px;object-fit:cover">` : ''}
                <div class="card-body text-start">
                    <div class="d-flex justify-content-between mb-2"><span class="badge bg-${cls}"><i class="bi bi-${icon}"></i> ${name}</span>
                        <small class="text-muted">${p.isPinned ? '📌 ' : ''}${fmt(p.publishedAt)}${p.groupName ? ' · ' + esc(p.groupName) : ''}</small></div>
                <h5 class="card-title">${esc(p.title)}</h5>
                    ${p.eventDate ? `<div class="text-muted mb-2"><i class="bi bi-calendar-event"></i> ${fmtDT(p.eventDate)}${p.location ? ` · <i class="bi bi-geo-alt"></i> ${esc(p.location)}` : ''}</div>` : ''}
                    <p class="card-text">${md(long ? p.body.slice(0, 400) + '…' : p.body)}</p>
                    <div class="d-flex justify-content-between">
                        <small class="text-muted">${esc(p.authorName)}</small>
                        ${long ? '<small class="text-primary">Читать полностью →</small>' : ''}
                    </div>
                </div></div>`;
        }).join('') : '<div class="text-muted">Публикаций пока нет</div>';

        const eventsEl = document.getElementById('events');
        if (!type && eventsEl) {
            const ev = posts.filter(p => p.eventDate && new Date(p.eventDate) >= new Date())
                .sort((a, b) => new Date(a.eventDate) - new Date(b.eventDate)).slice(0, 6);
            eventsEl.innerHTML = ev.length ? ev.map(p =>
                `<li class="list-group-item" data-id="${p.id}" role="button"><div class="fw-semibold">${esc(p.title)}</div><small class="text-muted">${fmtDT(p.eventDate)}${p.location ? ' · ' + esc(p.location) : ''}</small></li>`
            ).join('') : '<li class="list-group-item text-muted">Событий нет</li>';
        }

        // открыть пост из адреса вида /#post-15
        const m = location.hash.match(/^#post-(\w+)$/);
        if (m && cache.has(m[1])) openPost(m[1]);
    }

    // один делегированный обработчик на ленту и список событий
    document.addEventListener('click', e => {
        const el = e.target.closest('[data-id]');
        if (el && (el.closest('#feed') || el.closest('#events'))) openPost(el.dataset.id);
    });

    document.querySelectorAll('#typeFilter button').forEach(b => b.addEventListener('click', () => {
        document.querySelectorAll('#typeFilter button').forEach(x => x.classList.remove('active'));
        b.classList.add('active');
        load(b.dataset.type);
    }));
    load();
})();