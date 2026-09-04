(() => {
    const $ = id => document.getElementById(id);
    const REASON = { Sick: 'болезнь', Excused: 'предупредили', Late: 'опоздание', Unknown: 'без причины' };
    const MONTHS = ['янв', 'фев', 'мар', 'апр', 'май', 'июн', 'июл', 'авг', 'сен', 'окт', 'ноя', 'дек'];
    let chMonths, chReasons;

    fetch('/api/parent/children').then(r => r.json()).then(cs => {
        const c = cs.find(x => x.id === window.playerId);
        if (c) $('hTitle').textContent = `Посещаемость — ${c.name}`;
    });

    async function load() {
        const qs = new URLSearchParams(); if ($('fFrom').value) qs.set('from', $('fFrom').value); if ($('fTo').value) qs.set('to', $('fTo').value);
        const d = await (await fetch(`/api/parent/children/${window.playerId}/attendance?${qs}`)).json();
        const s = d.stats;
        $('sPercent').textContent = s.percent + '%'; $('sTotal').textContent = s.total; $('sPresent').textContent = s.present; $('sAbsent').textContent = s.absent;
        $('sPercent').className = `fs-2 fw-bold ${s.percent >= 80 ? 'text-success' : s.percent >= 60 ? 'text-warning' : 'text-danger'}`;

        chMonths?.destroy(); chReasons?.destroy();
        chMonths = new Chart($('chMonths'), {
            type: 'bar', data: {
                labels: d.byMonth.map(m => { const [y, mo] = m.month.split('-'); return `${MONTHS[+mo - 1]} ${y.slice(2)}`; }),
                datasets: [{ label: 'Посещено', data: d.byMonth.map(m => m.present), backgroundColor: '#198754' },
                { label: 'Пропущено', data: d.byMonth.map(m => m.total - m.present), backgroundColor: '#dc3545' }]
            },
            options: { scales: { x: { stacked: true }, y: { stacked: true, ticks: { precision: 0 } } }, plugins: { legend: { position: 'bottom' } } }
        });
        chReasons = new Chart($('chReasons'), {
            type: 'doughnut', data: {
                labels: ['Болезнь', 'Предупредили', 'Опоздание', 'Без причины'], datasets: [{ data: [s.sick, s.excused, s.late, s.unknown], backgroundColor: ['#0dcaf0', '#ffc107', '#6f42c1', '#dc3545'] }]
            },
            options: { plugins: { legend: { position: 'bottom' } } }
        });

        $('rows').innerHTML = d.rows.length ? d.rows.map(r => `<tr>
            <td>${new Date(r.startsAt).toLocaleDateString('ru-RU')} ${new Date(r.startsAt).toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit' })}${r.kind === 'Match' ? ' ⚽' : ''}</td>
            <td>${r.venueName}</td>
            <td class="text-center">${r.present ? '<span class="badge bg-success">был</span>' : `<span class="badge bg-danger">нет</span> <small class="text-muted">${REASON[r.reason] ?? ''}${r.noticedInAdvance ? ', вы предупредили' : ''}</small>`}</td>
            <td>${r.coachComment ?? ''}</td><td class="small text-muted">${r.highlights ?? ''}</td></tr>`).join('')
            : '<tr><td colspan="5" class="text-center text-muted py-4">Проведённых тренировок пока нет</td></tr>';
    }
    $('btnApply').addEventListener('click', load);
    load();
})();