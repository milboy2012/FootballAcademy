(() => {
    const $ = id => document.getElementById(id);
    const json = (u, m, b) => fetch(u, { method: m, headers: { 'Content-Type': 'application/json' }, body: b ? JSON.stringify(b) : undefined });
    const fmtT = d => new Date(d).toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit' });
    let children = [], notices = {}; // playerId -> Set(trainingId)
    let current = null, currentChild = null;

    const childrenOfGroup = groupId => children.filter(c => c.groupId === groupId);

    async function init() {
        children = await (await fetch('/api/parent/children')).json();
        // groupId нет в ChildBriefDto — добавьте его в DTO; здесь предполагаю поле groupId
        children.forEach(c => $('fChild').add(new Option(c.name, c.id)));
        $('legend').innerHTML = children.map(c => `<span class="badge me-1" style="background:${c.color ?? '#3788d8'}">${c.name}${c.groupName ? ' · ' + c.groupName : ''}</span>`).join('');
        for (const c of children) notices[c.id] = new Set(await (await fetch(`/api/parent/children/${c.id}/notices`)).json());
        calendar.render();
    }

    const calendar = new FullCalendar.Calendar($('calendar'), {
        locale: 'ru', firstDay: 1, height: 'auto', allDaySlot: false, slotMinTime: '08:00:00', slotMaxTime: '22:00:00',
        initialView: window.innerWidth < 768 ? 'listWeek' : 'timeGridWeek',
        headerToolbar: { left: 'prev,next today', center: 'title', right: 'dayGridMonth,timeGridWeek,listWeek' },
        events: { url: '/api/schedule', extraParams: () => { const c = children.find(x => x.id === $('fChild').value); return c ? { groupId: c.groupId } : {}; } },
        eventContent: arg => {
            const p = arg.event.extendedProps;
            const kids = childrenOfGroup(p.groupId);
            const noticed = kids.some(k => notices[k.id]?.has(arg.event.id));
            return {
                html: `<div class="fc-event-main-frame"><div class="fc-event-time">${fmtT(arg.event.start)}</div>
                <div class="fc-event-title">${noticed ? '🚫 ' : ''}${p.kind === 'Match' ? '⚽ ' : ''}${kids.map(k => k.name.split(' ')[0]).join(', ') || p.groupName}<br><small>${p.venueName}</small></div></div>`
            };
        },
        eventClick: info => open(info.event)
    });

    const modal = new bootstrap.Modal('#evModal');
    function open(ev) {
        current = ev; const p = ev.extendedProps;
        const kids = childrenOfGroup(p.groupId); currentChild = kids[0]; // если двое детей в одной группе — упростим: первый
        $('evTitle').textContent = `${p.kind === 'Match' ? 'Матч: ' : ''}${p.groupName}${p.opponentName ? ' — ' + p.opponentName : ''}`;
        $('evInfo').innerHTML = `<div><b>${ev.start.toLocaleDateString('ru-RU', { weekday: 'long', day: 'numeric', month: 'long' })}</b>, ${fmtT(ev.start)}–${fmtT(ev.end)}</div>
            <div>📍 ${p.venueName}</div><div>Тренер: ${p.coachName}</div>
            ${p.status === 'Cancelled' ? `<div class="text-danger mt-2">Отменено${p.cancelReason ? ': ' + p.cancelReason : ''}</div>` : ''}
            ${p.status === 'Completed' ? '<div class="text-success mt-2">Проведена</div>' : ''}`;
        const future = p.status === 'Planned' && ev.start > new Date();
        const noticed = currentChild && notices[currentChild.id].has(ev.id);
        $('noticeWrap').style.display = future && !noticed ? '' : 'none';
        $('btnNotice').style.display = future && !noticed ? '' : 'none';
        $('noticedInfo').classList.toggle('d-none', !noticed);
        $('nErr').classList.add('d-none'); $('nComment').value = '';
        modal.show();
    }

    $('btnNotice').addEventListener('click', async () => {
        const r = await json('/api/parent/absence', 'POST', { playerId: currentChild.id, trainingId: current.id, reason: $('nReason').value, comment: $('nComment').value || null });
        if (!r.ok) { $('nErr').textContent = (await r.json().catch(() => null))?.error ?? 'Ошибка'; $('nErr').classList.remove('d-none'); return; }
        notices[currentChild.id].add(current.id); modal.hide(); calendar.refetchEvents();
    });
    $('btnWithdraw').addEventListener('click', async e => {
        e.preventDefault();
        const r = await fetch(`/api/parent/absence/${currentChild.id}/${current.id}`, { method: 'DELETE' });
        if (r.ok) { notices[currentChild.id].delete(current.id); modal.hide(); calendar.refetchEvents(); }
    });
    $('fChild').addEventListener('change', () => calendar.refetchEvents());
    init();
})();