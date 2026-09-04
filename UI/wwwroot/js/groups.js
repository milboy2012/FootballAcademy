(() => {
    const $ = id => document.getElementById(id);
    const json = (url, method, body) => fetch(url, { method, headers: { 'Content-Type': 'application/json' }, body: body ? JSON.stringify(body) : undefined });
    const errorOf = async r => (await r.json().catch(() => null))?.error ?? `Ошибка ${r.status}`;
    const fmtDate = d => d ? new Date(d).toLocaleDateString('ru-RU') : '';

    let coaches = [], activeGroups = [], current = null;

    async function loadLookups() {
        coaches = await (await fetch('/api/groups/coaches')).json();
        const fc = $('fCoach'), gc = document.querySelector('#groupForm [name=coachId]');
        fc.length = 1; gc.length = 0;
        for (const c of coaches) {
            fc.add(new Option(`${c.name} (${c.groupsCount})`, c.id));
            if (c.isActive) gc.add(new Option(`${c.name} — групп: ${c.groupsCount}`, c.id));
        }
        const r = await (await fetch('/api/groups?archived=false&size=200')).json();
        activeGroups = r.data;
    }
    function fillTargets(select, exceptId, firstLabel) {
        select.length = 0; select.add(new Option(firstLabel, ''));
        for (const g of activeGroups) if (g.id !== exceptId) select.add(new Option(`${g.name} (${g.playersCount}/${g.maxPlayers})`, g.id));
    }

    // ---------- таблица групп ----------
    const table = new Tabulator('#groupsTable', {
        layout: 'fitColumns', selectable: 1,
        ajaxURL: '/api/groups',
        ajaxParams: () => ({ search: $('fSearch').value, coachId: $('fCoach').value, archived: document.querySelector('[name=arch]:checked').value }),
        pagination: true, paginationMode: 'remote', paginationSize: 15, sortMode: 'remote',
        initialSort: [{ column: 'name', dir: 'asc' }], placeholder: 'Групп нет',
        columns: [
            { title: '', field: 'color', width: 30, headerSort: false, formatter: c => `<span style="display:inline-block;width:14px;height:14px;border-radius:3px;background:${c.getValue() ?? '#ccc'}"></span>` },
            { title: 'Группа', field: 'name', minWidth: 90 },
            { title: 'Сезон', field: 'season', width: 100, headerSort: false },
            { title: 'Годы', field: 'minBirthYear', width: 110, formatter: c => `${c.getValue()}–${c.getRow().getData().maxBirthYear}` },
            { title: 'Тренер', field: 'coachName', minWidth: 150 },
            { title: 'Игроки', field: 'playersCount', width: 90, hozAlign: 'center', formatter: c => { const d = c.getRow().getData(); const full = d.playersCount >= d.maxPlayers; return `<span class="${full ? 'text-danger fw-bold' : ''}">${d.playersCount}/${d.maxPlayers}</span>`; } },
            { title: 'Трен.', field: 'upcomingTrainings', width: 70, hozAlign: 'center', headerSort: false, tooltip: 'Запланировано тренировок' },
            {
                title: '', field: 'id', width: 110, headerSort: false, hozAlign: 'center',
                formatter: c => c.getRow().getData().isArchived
                    ? '<button class="btn btn-sm btn-outline-success" data-act="unarchive" title="Восстановить"><i class="bi bi-arrow-counterclockwise"></i></button>'
                    : '<button class="btn btn-sm btn-outline-primary me-1" data-act="edit" title="Изменить"><i class="bi bi-pencil"></i></button>' +
                    '<button class="btn btn-sm btn-outline-warning" data-act="archive" title="В архив"><i class="bi bi-archive"></i></button>',
                cellClick: (e, cell) => {
                    const act = e.target.closest('button')?.dataset.act, d = cell.getRow().getData();
                    if (act === 'edit') openEdit(d);
                    if (act === 'archive') openArchive(d);
                    if (act === 'unarchive') unarchive(d);
                }
            }
        ]
    });
    table.on('rowClick', (e, row) => { if (!e.target.closest('button')) showRoster(row.getData()); });
    let t; const refresh = () => { clearTimeout(t); t = setTimeout(() => table.setData(), 300); };
    $('fSearch').addEventListener('input', refresh); $('fCoach').addEventListener('change', refresh);
    document.querySelectorAll('[name=arch]').forEach(r => r.addEventListener('change', () => { $('rosterCard').style.display = 'none'; table.setData(); }));

    // ---------- состав группы ----------
    const roster = new Tabulator('#rosterTable', {
        layout: 'fitColumns', height: '420px', selectable: true, placeholder: 'В группе нет игроков',
        rowFormatter: row => { const d = row.getData(); if (!d.medicalValid) row.getElement().classList.add('table-warning'); },
        columns: [
            { formatter: 'rowSelection', titleFormatter: 'rowSelection', hozAlign: 'center', headerSort: false, width: 40, cellClick: (e, c) => c.getRow().toggleSelect() },
            { title: 'Игрок', field: 'lastName', formatter: c => `${c.getValue()} ${c.getRow().getData().firstName}` },
            { title: 'Возр.', field: 'age', width: 60, hozAlign: 'center' },
            { title: 'Справка', field: 'medicalValid', width: 80, hozAlign: 'center', formatter: c => c.getValue() ? '<i class="bi bi-check-circle text-success"></i>' : '<i class="bi bi-exclamation-triangle text-warning"></i>' },
            { title: 'Абон.', field: 'hasActiveSubscription', width: 70, hozAlign: 'center', formatter: 'tickCross' },
            { title: 'Посещ.', field: 'attendancePercent', width: 80, hozAlign: 'center', formatter: c => c.getValue() + '%' }
        ]
    });
    roster.on('rowSelectionChanged', rows => $('btnMove').disabled = rows.length === 0 || current?.isArchived);

    function showRoster(g) {
        current = g;
        $('rosterCard').style.display = '';
        $('rosterTitle').textContent = `${g.name} — ${g.coachName}`;
        $('btnPrintJournal').href = `/Groups/Print/${g.id}?mode=journal`;
        $('btnPrintParents').href = `/Groups/Print/${g.id}?mode=parents`;
        fillTargets($('moveTarget'), g.id, '— отчислить (без группы) —');
        roster.setData(`/api/groups/${g.id}/players`);
    }
    $('btnMove').addEventListener('click', async () => {
        const ids = roster.getSelectedData().map(p => p.id);
        const target = $('moveTarget').value || null;
        const label = target ? `Перевести ${ids.length} игрок(ов) в «${$('moveTarget').selectedOptions[0].text}»?` : `Отчислить ${ids.length} игрок(ов) из группы?`;
        if (!confirm(label)) return;
        const r = await json(`/api/groups/${current.id}/players/move`, 'POST', { playerIds: ids, targetGroupId: target });
        if (!r.ok) return alert(await errorOf(r));
        await loadLookups(); table.setData(); showRoster(current);
    });

    // ---------- форма ----------
    const modal = new bootstrap.Modal('#groupModal'), form = $('groupForm'), err = $('formError');
    const y = new Date().getFullYear();
    function openCreate() {
        form.reset(); form.classList.remove('was-validated'); err.classList.add('d-none');
        form.id.value = ''; form.minBirthYear.value = y - 10; form.maxBirthYear.value = y - 9;
        form.season.value = `${y}/${y + 1}`;
        modal.show();
    }
    function openEdit(g) {
        form.reset(); form.classList.remove('was-validated'); err.classList.add('d-none');
        form.id.value = g.id; form.name.value = g.name; form.season.value = g.season ?? '';
        form.minBirthYear.value = g.minBirthYear; form.maxBirthYear.value = g.maxBirthYear; form.maxPlayers.value = g.maxPlayers;
        form.coachId.value = g.coachId; form.color.value = g.color ?? '#3788d8'; form.description.value = g.description ?? '';
        modal.show();
    }
    form.addEventListener('submit', async e => {
        e.preventDefault();
        if (!form.checkValidity()) return form.classList.add('was-validated');
        const dto = {
            name: form.name.value, season: form.season.value || null,
            minBirthYear: +form.minBirthYear.value, maxBirthYear: +form.maxBirthYear.value, maxPlayers: +form.maxPlayers.value,
            coachId: form.coachId.value, color: form.color.value, description: form.description.value || null
        };
        const id = form.id.value;
        const r = await json(id ? `/api/groups/${id}` : '/api/groups', id ? 'PUT' : 'POST', dto);
        if (!r.ok) { err.textContent = await errorOf(r); err.classList.remove('d-none'); return; }
        modal.hide(); await loadLookups(); table.setData();
    });
    $('btnAdd').addEventListener('click', openCreate);

    // ---------- архив ----------
    const archiveModal = new bootstrap.Modal('#archiveModal'); let archiving = null;
    function openArchive(g) { archiving = g; fillTargets($('archiveMoveTo'), g.id, 'Освободить (без группы)'); $('archiveError').classList.add('d-none'); archiveModal.show(); }
    $('btnArchiveConfirm').addEventListener('click', async () => {
        const moveTo = $('archiveMoveTo').value;
        const r = await fetch(`/api/groups/${archiving.id}/archive${moveTo ? '?moveTo=' + moveTo : ''}`, { method: 'POST' });
        if (!r.ok) { $('archiveError').textContent = await errorOf(r); $('archiveError').classList.remove('d-none'); return; }
        archiveModal.hide(); $('rosterCard').style.display = 'none'; await loadLookups(); table.setData();
    });
    async function unarchive(g) {
        if (!confirm(`Восстановить группу «${g.name}»?`)) return;
        const r = await fetch(`/api/groups/${g.id}/unarchive`, { method: 'POST' });
        r.ok ? (await loadLookups(), table.setData()) : alert(await errorOf(r));
    }

    loadLookups();
})();