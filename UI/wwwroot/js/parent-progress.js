(() => {
    const $ = id => document.getElementById(id);
    const PALETTE = ['#0d6efd', '#dc3545', '#198754', '#fd7e14', '#6f42c1', '#20c997', '#ffc107', '#6610f2'];
    const fmtD = d => new Date(d).toLocaleDateString('ru-RU');

    fetch('/api/parent/children').then(r => r.json()).then(cs => { const c = cs.find(x => x.id === window.playerId); if (c) $('hTitle').textContent = `Успеваемость — ${c.name}`; });

    fetch(`/api/parent/children/${window.playerId}/progress`).then(r => r.json()).then(d => {
        $('hSeason').textContent = d.season ? `Сезон ${d.season}` : '';
        if (!d.assessments.length) { $('empty').classList.remove('d-none'); $('content').classList.add('d-none'); return; }

        const labels = d.skills.map(s => s.name);
        const vals = a => d.skills.map(s => a?.scores[s.id] ?? null);
        const avg = arr => { const v = arr.filter(x => x != null); return v.length ? (v.reduce((a, b) => a + b, 0) / v.length).toFixed(1) : '–'; };
        const sameAssessment = d.seasonStart && d.latest && d.seasonStart.id === d.latest.id;

        // Радар
        const datasets = [{ label: `Сейчас (${fmtD(d.latest.date)})`, data: vals(d.latest), borderColor: '#0d6efd', backgroundColor: 'rgba(13,110,253,.25)' }];
        if (!sameAssessment) datasets.push({ label: `Начало сезона (${fmtD(d.seasonStart.date)})`, data: vals(d.seasonStart), borderColor: '#adb5bd', backgroundColor: 'rgba(173,181,189,.15)', borderDash: [5, 5] });
        if (d.groupAverage) datasets.push({ label: 'Среднее по группе', data: d.skills.map(s => d.groupAverage[s.id] ?? null), borderColor: '#198754', backgroundColor: 'transparent', pointStyle: 'triangle' });
        new Chart($('chRadar'), { type: 'radar', data: { labels, datasets }, options: { scales: { r: { min: 0, max: 10, ticks: { stepSize: 2 } } }, plugins: { legend: { position: 'bottom' } } } });

        // Таблица прогресса
        $('#tblProgress tbody' && 'tblProgress').querySelector('tbody').innerHTML = d.skills.map(s => {
            const a = d.seasonStart?.scores[s.id], b = d.latest.scores[s.id], g = d.groupAverage?.[s.id];
            const diff = a != null && b != null && !sameAssessment ? b - a : null;
            const cls = diff > 0 ? 'text-success' : diff < 0 ? 'text-danger' : 'text-muted';
            return `<tr><td>${s.name}</td><td class="text-center">${sameAssessment ? '–' : a ?? '–'}</td><td class="text-center fw-bold">${b ?? '–'}</td>
                <td class="text-center ${cls}">${diff == null ? '–' : (diff > 0 ? '▲ +' : diff < 0 ? '▼ ' : '') + diff}</td>
                <td class="text-center text-muted">${g ?? '–'}${g != null && b != null ? (b > g ? ' <span class="text-success">↑</span>' : b < g ? ' <span class="text-danger">↓</span>' : '') : ''}</td></tr>`;
        }).join('');
        $('avgLine').textContent = `Средний балл: ${avg(vals(d.latest))}${sameAssessment ? '' : ` (в начале сезона ${avg(vals(d.seasonStart))})`}${d.groupAverage ? ` · по группе ${avg(d.skills.map(s => d.groupAverage[s.id]))}` : ''}`;

        // Линии по датам
        new Chart($('chLines'), {
            type: 'line', data: {
                labels: d.assessments.map(a => fmtD(a.date)),
                datasets: d.skills.map((s, i) => ({ label: s.name, data: d.assessments.map(a => a.scores[s.id] ?? null), borderColor: PALETTE[i % PALETTE.length], tension: .3, spanGaps: true }))
            },
            options: { scales: { y: { min: 0, max: 10 } }, plugins: { legend: { position: 'bottom' } } }
        });

        // Комментарии
        const withComments = [...d.assessments].reverse().filter(a => a.comment);
        $('comments').innerHTML = withComments.length ? withComments.map(a => `<li class="list-group-item"><div class="d-flex justify-content-between"><b>${fmtD(a.date)}</b><small class="text-muted">${a.coachName}</small></div>${a.comment}</li>`).join('')
            : '<li class="list-group-item text-muted">Комментариев нет</li>';
    });
})();