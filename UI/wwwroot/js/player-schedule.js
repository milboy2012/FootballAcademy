(() => {
    const $ = id => document.getElementById(id);
    const REASON = { Sick: 'болею', Excused: 'не смогу прийти', Late: 'опоздаю' };
    const fmtT = d => new Date(d).toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit' });
    const modal = new bootstrap.Modal('#nModal');
    let items = [], current = null;

    const dayLabel = d => {
        const dt = new Date(d), today = new Date(); today.setHours(0, 0, 0, 0);
        const diff = Math.round((new Date(dt.getFullYear(), dt.getMonth(), dt.getDate()) - today) / 86400000);
        const base = dt.toLocaleDateString('ru-RU', { weekday: 'long', day: 'numeric', month: 'long' });
        return diff === 0 ? `Сегодня, ${base}` : diff === 1 ? `Завтра, ${base}` : base[0].toUpperCase() + base.slice(1);
    };

    async function load() {
        const r = await fetch('/api/me/upcoming?days=14');
        if (!r.ok) { $('list').innerHTML = '<div class="alert alert-warning">Учётная запись не привязана к ученику</div>'; return; }
        items = await r.json();
        if (!items.length) { $('list').innerHTML = '<div class="alert alert-light">На две недели занятий нет</div>'; return; }

        let lastDay = '';
        $('list').innerHTML = items.map(t => {
            const day = dayLabel(t.startsAt);
            const header = day !== lastDay ? `<h6 class="text-muted mt-3 mb-1">${day}</h6>` : ''; lastDay = day;
            const cancelled = t.status === 'Cancelled';
            const title = t.kind === 'Match' ? `⚽ Матч${t.opponentName ? ' с ' + t.opponentName : ''}` : 'Тренировка';
            const badge = cancelled ? `<span class="badge bg-secondary">отменено${t.cancelReason ? ': ' + t.cancelReason : ''}</span>`
                : t.noticed ? `<span class="badge bg-warning text-dark">предупредил(а) ${t.noticedBy}: ${REASON[t.noticeReason] ?? ''}</span>` : '';
            const action = cancelled ? '' : t.noticed
                ? (t.noticedBy === 'ты' ? `<button class="btn btn-sm btn-outline-success" data-withdraw="${t.id}">Всё-таки приду</button>` : '')
                : `<button class="btn btn-sm btn-outline-warning" data-notice="${t.id}">Не приду</button>`;
            return `${header}<div class="card ${cancelled ? 'opacity-50' : ''}"><div class="card-body d-flex align-items-center gap-3 py-2">
                <div class="text-center" style="min-width:64px"><div class="fs-4 fw-bold">${fmtT(t.startsAt)}</div><small class="text-muted">до ${fmtT(t.endsAt)}</small></div>
                <div class="flex-grow-1"><div class="fw-semibold">${title}</div><div class="text-muted small">📍 ${t.venueName}${t.venueAddress ? ', ' + t.venueAddress : ''}</div>${badge}</div>
                ${action}</div></div>`;
        }).join('');

        document.querySelectorAll('[data-notice]').forEach(b => b.addEventListener('click', () => open(b.dataset.notice)));
        document.querySelectorAll('[data-withdraw]').forEach(b => b.addEventListener('click', async () => {
            const r = await fetch(`/api/me/absence/${b.dataset.withdraw}`, { method: 'DELETE' });
            r.ok ? load() : alert((await r.json().catch(() => null))?.error ?? 'Ошибка');
        }));
    }

    function open(id) {
        current = items.find(t => t.id === id);
        $('nWhen').textContent = `${dayLabel(current.startsAt)}, ${fmtT(current.startsAt)} · ${current.venueName}`;
        $('rExcused').checked = true; $('nComment').value = ''; $('nErr').classList.add('d-none');
        modal.show();
    }

    $('btnSend').addEventListener('click', async () => {
        const r = await fetch('/api/me/absence', {
            method: 'POST', headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ trainingId: current.id, reason: document.querySelector('[name=reason]:checked').value, comment: $('nComment').value || null })
        });
        if (!r.ok) { $('nErr').textContent = (await r.json().catch(() => null))?.error ?? 'Ошибка'; $('nErr').classList.remove('d-none'); return; }
        modal.hide(); load();
    });
    load();
})();