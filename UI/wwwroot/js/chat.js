(() => {
    const $ = id => document.getElementById(id);
    const { me, group: initGroup, isModerator } = window.chatInit;
    const ROLE = { Coach: 'тренер', Manager: 'менеджер', Admin: 'админ', Parent: 'родитель', Player: 'игрок' };
    const esc = s => (s ?? '').replace(/[&<>"]/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' }[c]));
    const fmtT = d => new Date(d).toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit' });
    const fmtDay = d => new Date(d).toLocaleDateString('ru-RU', { day: 'numeric', month: 'long' });
    let groups = [], current = null, replyTo = null, oldest = null, loadingMore = false;

    const conn = new signalR.HubConnectionBuilder().withUrl('/hubs/chat').withAutomaticReconnect().build();

    // ---- список групп ----
    async function loadGroups() {
        groups = await (await fetch('/api/chat/groups')).json();
        $('groups').innerHTML = groups.map(g => `<a href="#" class="list-group-item list-group-item-action ${current === g.id ? 'active' : ''}" data-g="${g.id}">
            <div class="d-flex justify-content-between"><span><span class="d-inline-block rounded-circle me-1" style="width:10px;height:10px;background:${g.color ?? '#3788d8'}"></span><b>${esc(g.name)}</b></span>
            ${g.unread ? `<span class="badge bg-danger rounded-pill">${g.unread}</span>` : ''}</div>
            <div class="small text-truncate ${current === g.id ? '' : 'text-muted'}">${g.lastText ? esc(g.lastText) : 'нет сообщений'}</div></a>`).join('') || '<div class="p-3 text-muted">Нет доступных групп</div>';
        document.querySelectorAll('[data-g]').forEach(a => a.addEventListener('click', e => { e.preventDefault(); open(a.dataset.g); }));
    }

    // ---- открыть группу ----
    async function open(id) {
        current = id; replyTo = null; $('replyBox').classList.add('d-none'); oldest = null;
        const g = groups.find(x => x.id === id);
        $('gTitle').textContent = g.name; $('gSub').textContent = `тренер ${g.coachName} · участников: ${g.members}`;
        ['text', 'btnSend', 'btnMembers'].forEach(x => $(x).disabled = false);
        $('msgs').innerHTML = '';
        const msgs = await (await fetch(`/api/chat/${id}/messages?take=50`)).json();
        msgs.forEach(m => append(m, false)); scrollBottom();
        oldest = msgs[0]?.createdAt ?? null;
        await conn.invoke('MarkRead', id); loadGroups();
        history.replaceState(null, '', `/Chat?group=${id}`);
    }

    // ---- рендер ----
    let lastDay = null;
    function render(m) {
        const mine = m.senderId === me;
        return `<div class="d-flex mb-2 ${mine ? 'mine' : 'theirs'}" data-id="${m.id}">
            ${!mine ? `<div class="me-2">${m.senderAvatar ? `<img src="${m.senderAvatar}" class="rounded-circle" style="width:32px;height:32px;object-fit:cover">` : `<div class="rounded-circle bg-secondary text-white d-flex align-items-center justify-content-center" style="width:32px;height:32px">${esc(m.senderName[0])}</div>`}</div>` : ''}
            <div class="bubble">
                ${!mine ? `<div class="small fw-semibold role-${m.senderRole}">${esc(m.senderName)} <span class="text-muted fw-normal">· ${ROLE[m.senderRole] ?? ''}</span></div>` : ''}
                ${m.replyToId ? `<div class="reply"><b>${esc(m.replyToSender)}</b>: ${esc(m.replyToText).slice(0, 80)}</div>` : ''}
                <div class="text">${esc(m.text).replace(/\n/g, '<br>')}</div>
                <div class="small text-muted text-end">${m.isEdited ? 'изм. · ' : ''}${fmtT(m.createdAt)}
                    <a href="#" class="ms-2 text-decoration-none" data-act="reply" title="Ответить">↩</a>
                    ${mine ? '<a href="#" class="ms-1 text-decoration-none" data-act="edit" title="Изменить">✎</a>' : ''}
                    ${mine || isModerator ? '<a href="#" class="ms-1 text-decoration-none text-danger" data-act="del" title="Удалить">🗑</a>' : ''}
                </div></div></div>`;
    }
    function append(m, scroll = true) {
        const day = fmtDay(m.createdAt);
        if (day !== lastDay) { $('msgs').insertAdjacentHTML('beforeend', `<div class="text-center small text-muted my-2">${day}</div>`); lastDay = day; }
        $('msgs').insertAdjacentHTML('beforeend', render(m));
        if (scroll) scrollBottom();
    }
    const scrollBottom = () => $('msgs').scrollTop = $('msgs').scrollHeight;

    $('msgs').addEventListener('click', async e => {
        const a = e.target.closest('[data-act]'); if (!a) return; e.preventDefault();
        const el = a.closest('[data-id]'), id = el.dataset.id, text = el.querySelector('.text').innerText;
        if (a.dataset.act === 'reply') { replyTo = id; $('replyText').textContent = text.slice(0, 80); $('replyBox').classList.remove('d-none'); $('text').focus(); }
        if (a.dataset.act === 'edit') { const t = prompt('Изменить сообщение:', text); if (t !== null && t.trim()) conn.invoke('Edit', id, t); }
        if (a.dataset.act === 'del') { if (confirm('Удалить сообщение?')) conn.invoke('Delete', id); }
    });
    $('replyCancel').addEventListener('click', e => { e.preventDefault(); replyTo = null; $('replyBox').classList.add('d-none'); });

    // подгрузка истории при прокрутке вверх
    $('msgs').addEventListener('scroll', async () => {
        if ($('msgs').scrollTop > 40 || !oldest || loadingMore) return;
        loadingMore = true;
        const more = await (await fetch(`/api/chat/${current}/messages?take=50&before=${encodeURIComponent(oldest)}`)).json();
        if (more.length) { const h = $('msgs').scrollHeight; lastDay = null; $('msgs').insertAdjacentHTML('afterbegin', more.map(render).join('')); $('msgs').scrollTop = $('msgs').scrollHeight - h; oldest = more[0].createdAt; }
        else oldest = null;
        loadingMore = false;
    });

    // ---- отправка ----
    $('sendForm').addEventListener('submit', async e => {
        e.preventDefault(); const t = $('text').value.trim(); if (!t || !current) return;
        await conn.invoke('Send', current, t, replyTo);
        $('text').value = ''; replyTo = null; $('replyBox').classList.add('d-none'); $('text').style.height = 'auto';
    });
    $('text').addEventListener('keydown', e => { if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); $('btnSend').click(); } });
    $('text').addEventListener('input', () => { $('text').style.height = 'auto'; $('text').style.height = Math.min($('text').scrollHeight, 120) + 'px'; });
    let typingT; $('text').addEventListener('input', () => { if (!current) return; clearTimeout(typingT); typingT = setTimeout(() => conn.invoke('Typing', current), 300); });

    // ---- события хаба ----
    conn.on('Message', m => {
        if (m.groupId === current) { append(m); conn.invoke('MarkRead', current); }
        loadGroups();
    });
    conn.on('Edited', m => { const el = document.querySelector(`[data-id="${m.id}"]`); if (el) el.outerHTML = render(m); });
    conn.on('Deleted', id => document.querySelector(`[data-id="${id}"]`)?.remove());
    conn.on('Typing', (gid, name) => { if (gid !== current) return; $('typing').textContent = `${name} печатает…`; clearTimeout($('typing').t); $('typing').t = setTimeout(() => $('typing').textContent = '', 2000); });
    conn.on('Error', msg => alert(msg));

    $('btnMembers').addEventListener('click', async () => {
        const list = await (await fetch(`/api/chat/${current}/members`)).json();
        $('membersList').innerHTML = list.map(m => `<li class="list-group-item d-flex justify-content-between"><span>${esc(m.name)}</span><small class="text-muted">${m.role}</small></li>`).join('');
        new bootstrap.Modal('#membersModal').show();
    });

    conn.start().then(async () => { await loadGroups(); if (initGroup && groups.some(g => g.id === initGroup)) open(initGroup); else if (groups.length === 1) open(groups[0].id); });
})();