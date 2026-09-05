(() => {
    const { isStaff, isAdmin } = window.playersPage;
    const fmtDate = d => d ? new Date(d).toLocaleDateString('ru-RU') : '';
    const today = new Date().toISOString().slice(0, 10);

    // ---------- справочники ----------
    let lookups = { groups: [], parents: [] };

    async function loadLookups() {
        // if (!isStaff) return;
        // const r = await fetch('/api/venues/list');
        // if (!r.ok) return;
        // lookups = await r.json();

        //const fGroup = document.getElementById('fGroup');
        //const fmGroup = document.querySelector('#playerForm [name=groupId]');
        //const fmParent = document.querySelector('#playerForm [name=parentId]');
        //for (const g of lookups.groups) {
        //    fGroup.add(new Option(g.name, g.id));
        //    fmGroup.add(new Option(g.name, g.id));
        //}
        //for (const p of lookups.parents) fmParent.add(new Option(p.name, p.id));
    }

    // ---------- таблица ----------
    const columns = [
        { title: 'Название', field: 'name', minWidth: 120 },
        { title: 'Адрес', field: 'address', minWidth: 120 },
        //{ title: 'Тип', field: 'isIndoor', width: 140, formatter: c => fmtDate(c.getValue()) },
        {
            title: 'Тип', field: 'isIndoor', width: 160,  formatter: c => {
                const r = c.getRow().getData();
                if (!r.isActive) return '<span class="badge bg-success">В помещении</span>';
                else return '<span class="badge bg-success">Открытая площадка</span>';
            }
        },
        {
            title: 'Вместимость', field: 'capacity', width: 100, hozAlign: 'center'
        },
        {
            title: 'Использование', field: 'isActive', width: 180, formatter: c => {
                const r = c.getRow().getData();
                if (!r.isActive) return '<span class="badge bg-success">Не используется</span>';
                else return '<span class="badge bg-success">Используется</span>';
            }
        },
        { title: 'Запланировано тренировок', field: 'upcomingTrainings', minWidth: 160 },
        // {
        //     title: '', field: 'id', width: 110, headerSort: false, hozAlign: 'center',
        //     formatter:  () =>
        //         '<button class="btn btn-sm btn-outline-primary me-1" data-act="edit" title="Изменить"><i class="bi bi-pencil"></i></button>' +
        //             '<button class="btn btn-sm btn-outline-warning" data-act="delete" title="Удалить><i class="bi bi-archive"></i></button>',
        //         cellClick: (e, cell) => {
        //             const act = e.target.closest('button')?.dataset.act, d = cell.getRow().getData();
        //             if (act === 'edit') openEdit(d);
        //             if (act === 'delete') openDelete(d);                    
        //         }            
        // }
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
                if (act === 'del') {
                    remove(cell.getRow().getData());
                    
                } ;
            }
        });
    }

    const table = new Tabulator('#venuesTable', {
        layout: 'fitColumns',
        columns: columns,
        ajaxURL: '/api/venues',
        //ajaxParams: () => ({
        //    search: document.getElementById('fSearch').value,
        //    groupId: document.getElementById('fGroup').value,
        //    isActive: document.getElementById('fActive').value
        //}),
        dataLoaded: function (data) {
            console.log('Загруженные данные:', data);
            console.log('Количество записей:', data.length);
        },

        pagination: true,
        paginationMode: 'remote',
        paginationSize: 20,
        paginationSizeSelector: [10, 20, 50, 100],
        sortMode: 'remote',
        initialSort: [{ column: 'name', dir: 'asc' }],
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
    //document.getElementById('fSearch').addEventListener('input', refresh);
    //document.getElementById('fGroup').addEventListener('change', refresh);
    //document.getElementById('fActive').addEventListener('change', refresh);

    // ---------- форма ----------
    if (!isStaff) { loadLookups(); return; }

    const modalEl = document.getElementById('venueModal');
    const modal = new bootstrap.Modal(modalEl);
    const form = document.getElementById('venueForm');
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
        form.name.value = row.name;
        form.address.value = row.address;
        form.capacity.value = row.capacity;
        form.description.value = row.description ?? '';        
        form.isIndoor.checked = row.isIndoor;
        form.isActive.checked = row.isActive;
        modal.show();
    }
    

    form.addEventListener('submit', async e => {
        e.preventDefault();
        if (!form.checkValidity()) { form.classList.add('was-validated'); return; }

        const id = form.id.value;
        const dto = {
            name: form.name.value,
            address: form.address.value,
            capacity: form.capacity.value,
            description: form.description.value,
            isIndoor: form.isIndoor.checked,
            isActive: form.isActive.checked
        };

        const r = await fetch(id ? `/api/venues/${id}` : '/api/venues', {
            method: id ? 'PUT' : 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(dto)
        });

        if (r.ok) { modal.hide(); table.setData(); return; }
        const body = await r.json().catch(() => null);
        showError(body?.error ?? body?.title ?? `Ошибка ${r.status}`);
    });

    async function remove(row) {
        if (!confirm(`Удалить место тренировки ${row.name} расположеное по адресу: ${row.address}?`)) return;
        const r = await fetch(`/api/venues/${row.id}`, { method: 'DELETE' });
        if (r.ok) table.setData(); else alert(`Ошибка ${r.status}`);
    }

    document.getElementById('btnAdd').addEventListener('click', openCreate);
    loadLookups();
})();