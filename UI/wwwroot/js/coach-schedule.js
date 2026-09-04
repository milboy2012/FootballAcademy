(() => {
    const $ = id => document.getElementById(id);
    const fmt = d => new Date(d).toLocaleString('ru-RU', { weekday: 'short', day: '2-digit', month: '2-digit', hour: '2-digit', minute: '2-digit' });

    fetch('/api/coach/groups').then(r => r.json()).then(gs => gs.forEach(g => $('fGroup').add(new Option(`${g.name} (${g.playersCount})`, g.id))));

    const calendar = new FullCalendar.Calendar($('calendar'), {
        locale: 'ru', initialView: window.innerWidth < 768 ? 'listWeek' : 'timeGridWeek', height: 'auto', firstDay: 1,
        slotMinTime: '08:00:00', slotMaxTime: '22:00:00', allDaySlot: false, nowIndicator: true,
        headerToolbar: { left: 'prev,next today', center: 'title', right: 'dayGridMonth,timeGridWeek,listWeek' },
        events: { url: '/api/schedule', extraParams: () => ({ groupId: $('fGroup').value }) },
        eventClick: info => { if (info.event.extendedProps.status !== 'Cancelled') location.href = `/Coach/Training/${info.event.id}`; },
        eventDidMount: info => {
            const p = info.event.extendedProps;
            if (p.status === 'Completed') info.el.style.opacity = '0.65';
            info.el.title = `${p.venueName}${p.status === 'Completed' ? ' · проведена' : ''}${p.cancelReason ? ' · отменено: ' + p.cancelReason : ''}`;
        }
    });
    calendar.render();
    $('fGroup').addEventListener('change', () => calendar.refetchEvents());

    fetch('/api/coach/upcoming?days=7').then(r => r.json()).then(list => {
        const now = Date.now();
        $('upcoming').innerHTML = list.length ? list.map(t => {
            const past = new Date(t.endsAt).getTime() < now;
            const badge = t.status === 'Completed' ? '<span class="badge bg-success">проведена</span>'
                : past && !t.hasAttendance ? '<span class="badge bg-warning text-dark">не отмечена</span>'
                    : t.kind === 'Match' ? '<span class="badge bg-danger">матч</span>' : '';
            return `<a href="/Coach/Training/${t.id}" class="list-group-item list-group-item-action d-flex justify-content-between align-items-center">
                <div><div class="fw-semibold">${t.groupName}</div><small class="text-muted">${fmt(t.startsAt)} · ${t.venueName}</small></div>${badge}</a>`;
        }).join('') : '<div class="list-group-item text-muted">На неделю занятий нет</div>';
    });
})();