(() => {
    const { isStaff, isAdmin } = window.playersPage;
    const fmtDate = d => d ? new Date(d).toLocaleDateString('ru-RU') : '';
    const today = new Date().toISOString().slice(0, 10);

    // ---------- справочники ----------
    let lookups = { groups: [], parents: [] };

    async function loadLookups() {
        if (!isStaff) return;
        const r = await fetch('/api/venues/list');
        if (!r.ok) return;
        lookups = await r.json();

        const fGroup = document.getElementById('fGroup');
        const fmGroup = document.querySelector('#playerForm [name=groupId]');
        const fmParent = document.querySelector('#playerForm [name=parentId]');
        for (const g of lookups.groups) {
            fGroup.add(new Option(g.name, g.id));
            fmGroup.add(new Option(g.name, g.id));
        }
        for (const p of lookups.parents) fmParent.add(new Option(p.name, p.id));
    }

    // ---------- таблица ----------
    const columns = [
        { title: 'Название', field: 'lastName', minWidth: 120 },
        { title: 'Адрес', field: 'firstName', minWidth: 120 },
        { title: 'Крытое', field: 'birthDate', width: 140, formatter: c => fmtDate(c.getValue()) },
        { title: 'Вместимость', field: 'age', width: 100, headerSort: false, hozAlign: 'center' },
        { title: 'Активно', field: 'groupName', width: 110 },
        { title: 'Запланировано тренировок', field: 'parentName', minWidth: 160 },        
        //{ title: 'Действия', field: 'isActive', width: 100, hozAlign: 'center', formatter: 'tickCross' }
    ];

    if (isStaff) {
        columns.push({
            title: '', field: 'id', width: 110, headerSort: false, hozAlign: 'center',
            formatter: () =>
                `<button class="btn btn-sm btn-outline-primary me-1" data-act="edit" title="Изменить"><i class="bi bi-pencil"></i></button>` +
                (isAdmin ? `<button class="btn btn-sm btn-outline-danger" data-act="del" title="Удалить"><i class="bi bi-trash"></i></button>` : ''),
            cellClick: (e, cell) => {
                const act = e.target.closest('button')?.dataset.act;
                if (act === 'edit') openEdit(cell.getRow().getData());
                if (act === 'del') remove(cell.getRow().getData());
            }
        });
    }

    const table = new Tabulator('#playersTable', {
        layout: 'fitColumns',
        columns,
        ajaxURL: '/api/players',
        ajaxParams: () => ({
            search: document.getElementById('fSearch').value,
            groupId: document.getElementById('fGroup').value,
            isActive: document.getElementById('fActive').value
        }),
        pagination: true,
        paginationMode: 'remote',
        paginationSize: 20,
        paginationSizeSelector: [10, 20, 50, 100],
        sortMode: 'remote',
        initialSort: [{ column: 'lastName', dir: 'asc' }],
        ajaxResponse: (url, params, response) => response, // { data, last_page } — совпадает с форматом Tabulator
        placeholder: 'Нет данных',
        locale: 'ru-ru',
        langs: {
            'ru-ru': {
                pagination: { first: '«', last: '»', prev: '‹', next: '›', page_size: 'На странице' }
            }
        }
    });

    // Фильтры: перезапрос с задержкой
    let t;
    const refresh = () => { clearTimeout(t); t = setTimeout(() => table.setData(), 300); };
    document.getElementById('fSearch').addEventListener('input', refresh);
    document.getElementById('fGroup').addEventListener('change', refresh);
    document.getElementById('fActive').addEventListener('change', refresh);

    // ---------- форма ----------
    if (!isStaff) { loadLookups(); return; }

    const modalEl = document.getElementById('playerModal');
    const modal = new bootstrap.Modal(modalEl);
    const form = document.getElementById('playerForm');
    const errBox = document.getElementById('formError');

    function showError(msg) { errBox.textContent = msg; errBox.classList.remove('d-none'); }

    function openCreate() {
        form.reset();
        form.id.value = '';
        form.isActive.checked = true;
        errBox.classList.add('d-none');
        modal.show();
    }

    function openEdit(row) {
        form.reset();
        errBox.classList.add('d-none');
        form.id.value = row.id;
        form.lastName.value = row.lastName;
        form.firstName.value = row.firstName;
        form.birthDate.value = row.birthDate;
        form.medicalCertificateUntil.value = row.medicalCertificateUntil ?? '';
        form.parentId.value = row.parentId;
        form.groupId.value = row.groupId ?? '';
        form.isActive.checked = row.isActive;
        modal.show();
    }

    form.addEventListener('submit', async e => {
        e.preventDefault();
        if (!form.checkValidity()) { form.classList.add('was-validated'); return; }

        const id = form.id.value;
        const dto = {
            lastName: form.lastName.value,
            firstName: form.firstName.value,
            birthDate: form.birthDate.value,
            medicalCertificateUntil: form.medicalCertificateUntil.value || null,
            parentId: form.parentId.value,
            groupId: form.groupId.value || null,
            note: form.note.value || null,
            isActive: form.isActive.checked
        };

        const r = await fetch(id ? `/api/players/${id}` : '/api/players', {
            method: id ? 'PUT' : 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(dto)
        });

        if (r.ok) { modal.hide(); table.setData(); return; }
        const body = await r.json().catch(() => null);
        showError(body?.error ?? body?.title ?? `Ошибка ${r.status}`);
    });

    async function remove(row) {
        if (!confirm(`Удалить ученика ${row.lastName} ${row.firstName}?`)) return;
        const r = await fetch(`/api/players/${row.id}`, { method: 'DELETE' });
        if (r.ok) table.setData(); else alert(`Ошибка ${r.status}`);
    }

    document.getElementById('btnAdd').addEventListener('click', openCreate);
    loadLookups();
})();