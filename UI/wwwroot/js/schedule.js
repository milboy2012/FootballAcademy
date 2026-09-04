(() => {
    const { canEdit } = window.schedulePage;
    const $ = id => document.getElementById(id);
    const json = (url, method, body) => fetch(url, { method, headers: { 'Content-Type': 'application/json' }, body: body ? JSON.stringify(body) : undefined });
    const fmt = d => new Date(d).toLocaleString('ru-RU', { day: '2-digit', month: '2-digit', hour: '2-digit', minute: '2-digit' });
    const toIso = (date, time) => new Date(`${date}T${time}`).toISOString();
    const pad = n => String(n).padStart(2, '0');
    const dateOf = d => `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
    const timeOf = d => `${pad(d.getHours())}:${pad(d.getMinutes())}`;

    let groups = [], venues = [];

    async function loadLookups() {
        groups = (await (await fetch('/api/groups?archived=false&size=200')).json()).data ?? [];
        venues = (await (await fetch('/api/venues?active=true')).json()).data ?? [];
        const fill = (sel, items, first) => { sel.length = 0; if (first) sel.add(new Option(first, '')); items.forEach(i => sel.add(new Option(i.name, i.id))); };
        fill($('fGroup'), groups, 'Все группы'); fill($('fVenue'), venues, 'Все места');
        if (canEdit) {
            fill(document.querySelector('[name=groupId]'), groups); fill(document.querySelector('[name=opponentGroupId]'), groups);
            fill(document.querySelector('[name=venueId]'), venues);
        }
    }

    // ---------- календарь ----------
    const initDate = new URLSearchParams(location.search).get('date') || undefined;
    const calendar = new FullCalendar.Calendar($('calendar'), {
        locale: 'ru', initialView: 'timeGridWeek', initialDate: initDate, height: 'auto',
        slotMinTime: '08:00:00', slotMaxTime: '22:00:00', allDaySlot: false, nowIndicator: true, firstDay: 1,
        headerToolbar: { left: 'prev,next today', center: 'title', right: 'dayGridMonth,timeGridWeek,timeGridDay,listWeek' },
        events: { url: '/api/schedule', extraParams: () => ({ groupId: $('fGroup').value, venueId: $('fVenue').value }) },
        editable: canEdit, eventDurationEditable: canEdit, selectable: canEdit,
        eventAllow: (_, ev) => ev.extendedProps.status === 'Planned',
        select: info => openCreate(info.start, info.end),
        eventClick: info => canEdit && info.event.extendedProps.status === 'Planned' ? openEdit(info.event) : showInfo(info.event),
        eventDrop: onMove, eventResize: onMove,
        eventDidMount: info => { info.el.title = `${info.event.extendedProps.venueName}\nТренер: ${info.event.extendedProps.coachName}${info.event.extendedProps.cancelReason ? '\nОтменено: ' + info.event.extendedProps.cancelReason : ''}`; }
    });
    calendar.render();
    $('fGroup').addEventListener('change', () => calendar.refetchEvents());
    $('fVenue').addEventListener('change', () => calendar.refetchEvents());

    async function onMove(info) {
        let series = false;
        if (info.event.extendedProps.seriesId) series = confirm('Перенести также все последующие события серии?\nОК — всю серию, Отмена — только это.');
        const r = await json(`/api/schedule/${info.event.id}/move`, 'PATCH', { start: info.event.start.toISOString(), end: info.event.end.toISOString(), applyToSeries: series });
        if (!r.ok) { const b = await r.json().catch(() => null); alert(b?.conflicts ? conflictsText(b.conflicts) : b?.error ?? `Ошибка ${r.status}`); info.revert(); return; }
        calendar.refetchEvents();
    }
    const conflictsText = list => 'Пересечения:\n' + list.map(c => `• ${c.what}: ${fmt(c.start)}–${new Date(c.end).toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit' })} (${c.where})`).join('\n');

    function showInfo(ev) {
        const p = ev.extendedProps;
        $('infoTitle').textContent = ev.title;
        $('infoBody').innerHTML = `<div>${fmt(ev.start)} – ${ev.end.toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit' })}</div>
            <div>Место: ${p.venueName}</div><div>Тренер: ${p.coachName}</div>
            ${p.note ? `<div class="text-muted">${p.note}</div>` : ''}${p.cancelReason ? `<div class="text-danger mt-2">Отменено: ${p.cancelReason}</div>` : ''}`;
        new bootstrap.Modal('#infoModal').show();
    }

    if (!canEdit) { loadLookups(); return; }

    // ---------- форма ----------
    const modal = new bootstrap.Modal('#evModal'), form = $('evForm'), err = $('evError'), conf = $('evConflicts');
    let editing = null;
    const kindIsMatch = () => form.kind.value === 'Match';
    form.kind.forEach?.(r => r.addEventListener('change', syncKind)); document.querySelectorAll('[name=kind]').forEach(r => r.addEventListener('change', syncKind));
    function syncKind() { $('oppWrap').style.display = kindIsMatch() ? '' : 'none'; $('recWrap').style.display = kindIsMatch() || editing ? 'none' : ''; if (kindIsMatch()) $('recOn').checked = false, $('recBody').style.display = 'none'; }
    $('recOn').addEventListener('change', () => $('recBody').style.display = $('recOn').checked ? '' : 'none');

    function reset() { form.reset(); form.classList.remove('was-validated'); err.classList.add('d-none'); conf.classList.add('d-none'); document.querySelectorAll('#weekdays input').forEach(c => c.checked = false); }
    function openCreate(start, end) {
        editing = null; reset();
        $('evTitle').textContent = 'Новое событие'; $('btnCancelEv').classList.add('d-none');
        const s = start ?? new Date(), e = end ?? new Date(s.getTime() + 90 * 60000);
        form.date.value = dateOf(s); form.startTime.value = timeOf(s); form.endTime.value = timeOf(e);
        const until = new Date(s); until.setMonth(until.getMonth() + 3); form.until.value = dateOf(until);
        $(`wd${s.getDay()}`).checked = true;
        syncKind(); modal.show();
    }
    function openEdit(ev) {
        editing = ev; reset(); const p = ev.extendedProps;
        $('evTitle').textContent = 'Редактирование'; $('btnCancelEv').classList.remove('d-none');
        form.id.value = ev.id; form.kind.value = p.kind; form.groupId.value = p.groupId; form.opponentGroupId.value = p.opponentGroupId ?? '';
        form.venueId.value = p.venueId; form.date.value = dateOf(ev.start); form.startTime.value = timeOf(ev.start); form.endTime.value = timeOf(ev.end); form.note.value = p.note ?? '';
        syncKind(); modal.show();
    }
    $('btnAdd').addEventListener('click', () => openCreate());

    form.addEventListener('submit', async e => {
        e.preventDefault();
        if (!form.checkValidity()) return form.classList.add('was-validated');
        err.classList.add('d-none'); conf.classList.add('d-none');
        const dto = {
            kind: form.kind.value, groupId: form.groupId.value, opponentGroupId: kindIsMatch() ? form.opponentGroupId.value || null : null,
            venueId: form.venueId.value, start: toIso(form.date.value, form.startTime.value), end: toIso(form.date.value, form.endTime.value),
            note: form.note.value || null, notifyParticipants: $('notifyOn').checked, skipConflicts: $('skipConf').checked,
            recurrence: !editing && $('recOn').checked ? { weekdays: [...document.querySelectorAll('#weekdays input:checked')].map(c => +c.value), until: form.until.value } : null
        };
        const r = await json(editing ? `/api/schedule/${editing.id}` : '/api/schedule', editing ? 'PUT' : 'POST', dto);
        const body = await r.json().catch(() => null);
        if (r.status === 409) { conf.innerHTML = conflictsText(body.conflicts).replace(/\n/g, '<br>') + (dto.recurrence ? '<br><small>Включите «Пропускать занятые слоты», чтобы создать остальные.</small>' : ''); conf.classList.remove('d-none'); return; }
        if (!r.ok) { err.textContent = body?.error ?? `Ошибка ${r.status}`; err.classList.remove('d-none'); return; }
        modal.hide(); calendar.refetchEvents();
        if (body?.skipped?.length) alert(`Создано: ${body.created}. Пропущено из-за занятости: ${body.skipped.length}`);
    });

    // ---------- отмена ----------
    const cancelModal = new bootstrap.Modal('#cancelModal');
    $('btnCancelEv').addEventListener('click', () => { $('cancelReason').value = ''; $('cancelSeries').checked = false; $('cancelSeriesWrap').style.display = editing.extendedProps.seriesId ? '' : 'none'; modal.hide(); cancelModal.show(); });
    $('btnCancelConfirm').addEventListener('click', async () => {
        const r = await json(`/api/schedule/${editing.id}/cancel`, 'POST', { reason: $('cancelReason').value || null, applyToSeries: $('cancelSeries').checked });
        if (!r.ok) return alert((await r.json().catch(() => null))?.error ?? `Ошибка ${r.status}`);
        cancelModal.hide(); calendar.refetchEvents();
    });

    loadLookups();
})();