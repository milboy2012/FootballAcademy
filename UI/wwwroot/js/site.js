// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
// (() => {
//     const $ = id => document.getElementById(id);

//     fetch('/api/notifications').then(r => r.json()).then(gs => {
//         const notifList = document.getElementById('notifList');

//         //notificationItem.setAttribute('data-id', notification.id);


//         gs.forEach(g => {
//             const notificationItem = document.createElement('div');
//             const date = new Date(g.createdAt);

//             const formattedDate = date.toLocaleString('ru-RU', {
//                 day: '2-digit',
//                 month: '2-digit',
//                 year: 'numeric',
//                 hour: '2-digit',
//                 minute: '2-digit'
//             });

//             notificationItem.className = 'dropdown-item d-flex align-items-start p-3 notification-item';
//             notificationItem.innerHTML = `
//             <div class="d-flex">
//                 <div class="flex-shrink-0 me-3">
//                     <i class="fas fa-bell text-primary fs-4"></i>
//                 </div>
//                 <div class="flex-grow-1">
//                     <div class="d-flex justify-content-between align-items-start">
//                         <h6 class="mb-1">${g.title}</h6>
//                         <small class="text-muted ms-2">${formattedDate}</small>
//                     </div>
//                     <p class="mb-1 small">${g.message}</p>
//                 </div>
//             </div>
//             <a href="${g.link}" class="stretched-link"></a>`;
//             notifList.insertBefore(notificationItem, notifList.firstChild);
//         });
//     });

//     var notif = document.getElementById('notifReadAll');
//     notif.addEventListener('click', async e => {

//     });

// })();


(() => {
    const API = {
        list: '/api/notifications',              // GET  -> [{id, title, text, url, createdAt, isRead}]
        read: id => `/api/notifications/${id}/read`, // POST
        readAll: '/api/notifications/read-all'      // POST
    };

    const $list = document.getElementById('notifList');
    const $badge = document.getElementById('notifBadge');
    const $readAll = document.getElementById('notifReadAll');
    const $bell = document.getElementById('notifBell');

    let items = [];

    const esc = s => String(s ?? '').replace(/[&<>"']/g,
        c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));

    const fmtDate = iso => new Date(iso).toLocaleString('ru-RU',
        { day: '2-digit', month: '2-digit', hour: '2-digit', minute: '2-digit' });

    function render() {
        if (!items.length) {
            $list.innerHTML = '<div class="text-center text-muted py-3 small">Нет уведомлений</div>';
        } else {
            $list.innerHTML = items.map(n => `
                <a href="${esc(n.link)}" data-id="${n.id}"
                   class="list-group-item list-group-item-action notif-item d-flex gap-2 px-3 py-2 ${n.isRead ? 'read' : 'unread'}">
                    <span class="notif-dot"></span>
                    <div class="flex-grow-1 min-w-0">
                        <div class="d-flex justify-content-between gap-2">
                            <span class="fw-semibold text-truncate">${esc(n.title)}</span>
                            <small class="text-muted text-nowrap">${fmtDate(n.createdAt)}</small>
                        </div>
                        ${n.text ? `<div class="small text-muted notif-text">${esc(n.text)}</div>` : ''}
                </div>
                </a>`).join('');
        }
        updateBadge();
    }

    function updateBadge() {
        const unread = items.filter(n => !n.isRead).length;
        $badge.textContent = unread > 99 ? '99+' : unread;
        $badge.classList.toggle('d-none', unread === 0);
        $readAll.disabled = unread === 0;
    }

    async function load() {
        try {
            const r = await fetch(API.list, { headers: { Accept: 'application/json' } });
            if (!r.ok) throw new Error(r.status);
            items = await r.json();
            render();
        } catch {
            $list.innerHTML = '<div class="text-center text-danger py-3 small">Не удалось загрузить</div>';
        }
    }

    // Клик по событию: сначала пометить прочитанным, затем перейти
    $list.addEventListener('click', async e => {
        const a = e.target.closest('a.notif-item');
        if (!a) return;
        const id = a.dataset.id;
        const n = items.find(x => String(x.id) === id);
        if (!n || n.isRead) return;              // уже прочитано — обычный переход

        e.preventDefault();
        n.isRead = true;
        a.classList.replace('unread', 'read');
        updateBadge();
        try {
            await fetch(API.read(id), { method: 'POST', keepalive: true });
        } finally {
            location.href = a.href;
        }
    });

    // Прочитать все
    $readAll.addEventListener('click', async () => {
        if (!items.some(n => !n.isRead)) return;
        $readAll.disabled = true;
        const r = await fetch(API.readAll, { method: 'POST' }).catch(() => null);
        if (r && r.ok) {
            items.forEach(n => n.isRead = true);
            render();
        } else {
            $readAll.disabled = false;
        }
    });

    // Загружаем при открытии дропдауна и один раз при старте (для бейджа)
    $bell.addEventListener('show.bs.dropdown', load);
    load();
})();