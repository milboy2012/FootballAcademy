(() => {
    const $ = id => document.getElementById(id);
    const id = window.trainingId;
    const REASONS = { Unknown: 'без причины', Sick: 'болезнь', Excused: 'предупредили', Late: 'опоздал' };
    const fmtT = d => new Date(d).toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit' });
    let data;

    const msg = (el, text) => { $('err').classList.add('d-none'); $('ok').classList.add('d-none'); if (text) { $(el).textContent = text; $(el).classList.remove('d-none'); } };

    function render() {
        const isMatch = data.kind === 'Match';
        $('hTitle').textContent = `${isMatch ? 'Матч ' : ''}${data.groupName}${isMatch && data.opponentName ? ' — ' + data.opponentName : ''}`;
        $('hSub').textContent = `${new Date(data.startsAt).toLocaleDateString('ru-RU', { weekday: 'long', day: 'numeric', month: 'long' })}, ${fmtT(data.startsAt)}–${fmtT(data.endsAt)} · ${data.venueName}`;
        const st = { Planned: ['bg-primary', 'запланирована'], Completed: ['bg-success', 'проведена'], Cancelled: ['bg-secondary', 'отменена'] }[data.status];
        $('hStatus').className = `badge fs-6 ${st[0]}`; $('hStatus').textContent = st[1];
        $('managerNote').textContent = data.note ? `Примечание менеджера: ${data.note}` : '';
        $('summary').value = data.summary ?? ''; $('highlights').value = data.highlights ?? '';

        $('rows').innerHTML = data.players.map(p => `
            <tr data-id="${p.playerId}" class="${p.medicalValid ? '' : 'table-warning'}">
                <td>
                    <div class="fw-semibold">${p.lastName} ${p.firstName}</div>
                    <small class="text-muted">${p.age ? p.age + ' лет · ' : ''}посещаемость ${p.attendancePercent}%
                        ${p.medicalValid ? '' : ' · <span class="text-danger">нет справки</span>'}${p.hasActiveSubscription ? '' : ' · <span class="text-warning">нет абонемента</span>'}</small>
                </td>
                <td class="text-center">
                    <div class="btn-group btn-group-sm" role="group">
                        <input type="radio" class="btn-check" name="p${p.playerId}" id="y${p.playerId}" value="1" ${p.present === true ? 'checked' : ''}><label class="btn btn-outline-success" for="y${p.playerId}">✓</label>
                        <input type="radio" class="btn-check" name="p${p.playerId}" id="n${p.playerId}" value="0" ${p.present === false ? 'checked' : ''}><label class="btn btn-outline-danger" for="n${p.playerId}">✕</label>
                    </div>
                </td>
                <td><select class="form-select form-select-sm reason" ${p.present === false ? '' : 'disabled'}>
                ${Object.entries(REASONS).map(([k, v]) => `<option value="${k}" ${p.reason === k ? 'selected' : ''}>${v}</option>`).join('')}</select></td>
                <td><input class="form-control form-control-sm comment" value="${p.comment ?? ''}" placeholder="…" /></td>
            </tr>`).join('');

        $('rows').querySelectorAll('input[type=radio]').forEach(r => r.addEventListener('change', e => {
            const tr = e.target.closest('tr'); tr.querySelector('.reason').disabled = e.target.value === '1'; count();
        }));
        const locked = data.status === 'Cancelled';
        document.querySelectorAll('#rows input, #rows select, #summary, #highlights, #btnDraft, #btnComplete, #btnAllPresent').forEach(el => el.disabled = locked);
        if (data.status === 'Completed') $('btnComplete').textContent = 'Сохранить изменения';
        count();
    }

    function count() {
        const rows = [...$('rows').querySelectorAll('tr')];
        const marked = rows.filter(tr => tr.querySelector('input:checked')).length;
        const present = rows.filter(tr => tr.querySelector('input[value="1"]:checked')).length;
        $('counter').textContent = `${present} из ${rows.length}${marked < rows.length ? ` · не отмечено ${rows.length - marked}` : ''}`;
    }

    function collect(complete) {
        return {
            complete,
            summary: $('summary').value || null,
            highlights: $('highlights').value || null,
            attendance: [...$('rows').querySelectorAll('tr')].filter(tr => tr.querySelector('input:checked')).map(tr => ({
                playerId: tr.dataset.id,
                present: tr.querySelector('input:checked').value === '1',
                reason: tr.querySelector('input:checked').value === '1' ? null : tr.querySelector('.reason').value,
                comment: tr.querySelector('.comment').value || null
            }))
        };
    }

    async function save(complete) {
        msg();
        const r = await fetch(`/api/coach/trainings/${id}/conduct`, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(collect(complete)) });
        if (!r.ok) return msg('err', (await r.json().catch(() => null))?.error ?? `Ошибка ${r.status}`);
        await load();
        msg('ok', complete ? 'Тренировка завершена, посещаемость сохранена' : 'Сохранено');
    }

    $('btnAllPresent').addEventListener('click', () => { $('rows').querySelectorAll('input[value="1"]').forEach(r => { r.checked = true; r.dispatchEvent(new Event('change')); }); });
    $('btnDraft').addEventListener('click', () => save(false));
    $('btnComplete').addEventListener('click', () => save(true));

    async function load() {
        const r = await fetch(`/api/coach/trainings/${id}`);
        if (!r.ok) { $('hTitle').textContent = 'Тренировка не найдена'; return; }
        data = await r.json(); render();
    }
    load();
})();