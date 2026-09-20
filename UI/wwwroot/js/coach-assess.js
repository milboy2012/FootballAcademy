(() => {
    const $ = id => document.getElementById(id);
    const id = window.trainingId; let data;
    const cell = (p, s) => `<td class="p-1 text-center" style="min-width:70px">
        <input type="number" min="1" max="10" class="form-control form-control-sm text-center score" data-p="${p.playerId}" data-s="${s.id}"
               value="${p.scores?.[s.id] ?? ''}" ${p.present ? '' : 'disabled'} placeholder="${p.avg5[s.id] ?? ''}" style="width:60px;margin:auto"></td>`;

    async function load() {
        const r = await fetch(`/api/coach/trainings/${id}/assessments`);
        if (!r.ok) {
            $('assessCard').style.display = 'none';
            return;
        }
        data = await r.json();
        $('assessTable').querySelector('thead').innerHTML = `<tr><th>Игрок</th>${data.skills.map(s => `<th class="text-center small" title="${s.description ?? ''}">${s.name}</th>`).join('')}<th>Комментарий</th></tr>`;
        $('assessTable').querySelector('tbody').innerHTML = data.players.map(p => `<tr class="${p.present ? '' : 'table-light text-muted'}">
            <td class="text-nowrap">${p.name}${p.present ? '' : ' <small>(не был)</small>'}</td>
            ${data.skills.map(s => cell(p, s)).join('')}
            <td><input class="form-control form-control-sm comment" data-p="${p.playerId}" value="${(p.comment ?? '').replace(/"/g, '&quot;')}" ${p.present ? '' : 'disabled'} placeholder="…"></td></tr>`).join('');
        // окраска по значению
        document.querySelectorAll('.score').forEach(i => { colour(i); i.addEventListener('input', () => colour(i)); });
    }
    function colour(i) { const v = +i.value; i.classList.remove('bg-success-subtle', 'bg-warning-subtle', 'bg-danger-subtle'); if (v >= 8) i.classList.add('bg-success-subtle'); else if (v >= 5) i.classList.add('bg-warning-subtle'); else if (v >= 1) i.classList.add('bg-danger-subtle'); }

    $('btnFillAvg').addEventListener('click', () => document.querySelectorAll('.score:not([disabled])').forEach(i => { if (!i.value && i.placeholder) { i.value = Math.round(+i.placeholder); colour(i); } }));

    $('btnSaveAssess').addEventListener('click', async () => {
        const players = data.players.filter(p => p.present).map(p => ({
            playerId: p.playerId,
            scores: Object.fromEntries([...document.querySelectorAll(`.score[data-p="${p.playerId}"]`)].filter(i => i.value).map(i => [i.dataset.s, +i.value])),
            comment: document.querySelector(`.comment[data-p="${p.playerId}"]`).value || null
        }));
        const r = await fetch(`/api/coach/trainings/${id}/assessments`, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ players }) });
        $('assessMsg').className = `small ${r.ok ? 'text-success' : 'text-danger'}`;
        $('assessMsg').textContent = r.ok ? 'Оценки сохранены' : ((await r.json().catch(() => null))?.error ?? 'Ошибка');
        if (r.ok) load();
    });

    load();
    // после сохранения посещаемости в coach-training.js вызывается window.reloadAssessments?.()
    window.reloadAssessments = load;
})();