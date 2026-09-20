(() => {
    let groups = [];
    const audienceData = [ {id: 0, name: 'всем'}, {id:1, name: 'зарегистрированным'}, {id:2 , name: 'родителям'}, {id : 3, name: 'тренерам'} ];
    const typeData = [ {id: 0, name: 'Объявление'}, {id:1, name: 'Новость'}, {id:2 , name: 'Праздник'}, {id : 3, name: 'Соревнование'} ];
    //const { isStaff, isAdmin } = window.playersPage;
    const json = (url, method, body) => fetch(url, { method, headers: { 'Content-Type': 'application/json' }, body: body ? JSON.stringify(body) : undefined });
    const $ = id => document.getElementById(id);
    //const toIso = (date, time) => new Date(`${date}T${time}`).toISOString();
    const fmtDate = d => d ? new Date(d).toLocaleDateString('ru-RU') : '';
    const fmtDateTime = d => d
        ? new Date(d).toLocaleString('ru-RU', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' })
        : '';
    
    const fill = (sel, items, first) => { 
        sel.length = 0; 
        if (first) sel.add(new Option(first, '')); 
        items.forEach(i => sel.add(new Option(i.name, i.id))); 
   };

    fetch('/api/groups?archived=false&size=200').then(r => r.json()).then(s => {
        console.log(s.data);
        s.data;
        fill(document.querySelector('[name=group]'), s.data, 'для всех');
    });

    const modalEl = document.getElementById('postModal');
    const modal = new bootstrap.Modal(modalEl);
    const form = document.getElementById('postForm');
    const errBox = document.getElementById('formError');
   
   //groups = (await (await fetch('/api/groups?archived=false&size=200')).json()).data ?? [];

    fill(document.querySelector('[name=type]'), typeData); 
    fill(document.querySelector('[name=audience]'), audienceData); 

    const toLocalInput = d => {
        if (!d) return '';
        const dt = new Date(d);
        if (isNaN(dt)) return '';
        const p = n => String(n).padStart(2, '0');
        return `${dt.getFullYear()}-${p(dt.getMonth() + 1)}-${p(dt.getDate())}T${p(dt.getHours())}:${p(dt.getMinutes())}`;
    };

    const fromLocalInput = v => v ? new Date(v).toISOString() : null;

    const bodyCounter = $('bodyCounter');
    form.body.addEventListener('input', () => bodyCounter.textContent = form.body.value.length);

   const TYPE = { 0: ['объявление', 'warning'], 1: ['новость', 'success'], 2: ['праздник', 'secondary'], 3: ['соревнование', 'info'] };
   const AUDIENCE = { 0: ['всем', 'warning'], 1: ['зарегистрированным', 'success'], 2: ['родителям', 'secondary'], 3: ['тренерам', 'info'] };

   // ---------- таблица ----------
    const columns = [
        {
            title: 'Тип', field: 'type', width: 160, formatter: c => {                 
                const [t, cls] = TYPE[c.getValue()]; 
                return `<span class="badge bg-${cls}">${t}</span>`; 
            }
        },
        { title: 'Заголовок', field: 'title', minWidth: 120 },
        // { title: 'Текст', field: 'body', minWidth: 120 },
        {
            title: 'Текст', field: 'body', minWidth: 200, formatter: c => {
                const v = c.getValue() ?? '';
                const short = v.length > 120 ? v.slice(0, 120) + '…' : v;
                return `<span title="${v.replace(/"/g, '&quot;')}">${short}</span>`;
            }
        },
        // { title: 'Дата события', field: 'eventDate', width: 120, formatter: c => fmtDate(c.getValue()) },
        { title: 'Дата события', field: 'eventDate', width: 150, formatter: c => fmtDateTime(c.getValue()) },
        { title: 'Место проведения', field: 'location', minWidth: 120 },
        {
            title: 'Категория', field: 'audience', width: 160,  formatter: c => {
                const [t, cls] = AUDIENCE[c.getValue()]; 
                return `<span class="badge bg-${cls}">${t}</span>`; 
            }
        },
        { title: 'Группа', field: 'groupId', minWidth: 150 },
        {
            title: 'Закреплено', field: 'isPinned', width: 180, formatter: c => {
                const r = c.getRow().getData();
                if (!r.isActive) return '<span class="badge bg-success">Нет</span>';
                else return '<span class="badge bg-success">Да</span>';
            }
        },

        {
            title: 'Опубликовано', field: 'isPublished', width: 180, formatter: c => {
                const r = c.getRow().getData();
                if (!r.isActive) return '<span class="badge bg-success">Нет</span>';
                else return '<span class="badge bg-success">Да</span>';
            }
        },

        {
            title: 'Уведомить?', field: 'notify', width: 180, formatter: c => {
                const r = c.getRow().getData();
                if (!r.isActive) return '<span class="badge bg-success">Нет</span>';
                else return '<span class="badge bg-success">Да</span>';
            }
        }       
        
    ];

    columns.push({
            title: '', field: 'id', width: 110, headerSort: false, hozAlign: 'center',
            formatter: () =>
                `<button class="btn btn-sm btn-outline-primary me-1" data-act="edit" title="Изменить"><i class="bi bi-pencil"></i></button>` +
                // (isAdmin ? `<button class="btn btn-sm btn-outline-danger" data-act="del" title="Удалить"><i class="bi bi-trash"></i></button>` : ''),
                `<button class="btn btn-sm btn-outline-danger" data-act="del" title="Удалить"><i class="bi bi-trash"></i></button>`,
            cellClick: (e, cell) => {
                const act = e.target.closest('button')?.dataset.act;
                if (act === 'edit') openEdit(cell.getRow().getData());
                if (act === 'del') {
                    remove(cell.getRow().getData());
                    
                };
            }
        });

    // if (isStaff) {
        
    // }

    const table = new Tabulator('#postsTable', {
        layout: 'fitColumns',
        columns: columns,
        ajaxURL: '/api/posts',
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


    function openEdit(row) {
        form.reset();
        form.classList.remove('was-validated');
        errBox.classList.add('d-none');
        form.id.value = row.id;
        form.type.value = row.type;
        form.title.value = row.title ?? '';
        form.body.value = row.body ?? '';
        form.eventDate.value = toLocalInput(row.eventDate);
        form.location.value = row.location ?? '';
        form.audience.value = row.audience;
        form.group.value = row.groupId ?? '';
        form.isPinned.checked = !!row.isPinned;
        form.isPublished.checked = !!row.isPublished;
        form.notify.checked = !!row.notify;
        bodyCounter.textContent = form.body.value.length;
        modal.show();
    }

    document.getElementById('btnAdd').addEventListener('click', openCreate);
    function openCreate() {
        form.reset();
        form.classList.remove('was-validated');
        form.id.value = '';
        form.isPublished.checked = true;
        bodyCounter.textContent = 0;
        errBox.classList.add('d-none');
        modal.show();
    }

    form.addEventListener('submit', async e => {
        e.preventDefault();
        if (!form.checkValidity()) { form.classList.add('was-validated'); return; }

        const id = form.id.value;
        const dto = {
            type: +form.type.value,
            title: form.title.value.trim(),
            body: form.body.value,
            eventDate: fromLocalInput(form.eventDate.value),
            location: form.location.value.trim() || null,
            audience: +form.audience.value,
            isPinned: form.isPinned.checked,
            isPublished: form.isPublished.checked,
            groupId: form.group.value ? +form.group.value : null,
            notify: form.notify.checked
        };
        console.log(JSON.stringify(dto));
        const r = await fetch(id ? `/api/posts/${id}` : '/api/posts', {
            method: id ? 'PUT' : 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(dto)
        });

        if (r.ok) { modal.hide(); table.setData(); return; }
        const body = await r.json().catch(() => null);
        showError(body?.error ?? body?.title ?? `Ошибка ${r.status}`);
    });

    async function remove(row) {
        console.log(TYPE[row.type][0]);
        if (!confirm(`Удалить ${TYPE[row.type][0]} ${row.title} от ${fmtDate(row.eventDate)}?`)) return;
        const r = await fetch(`/api/posts/${row.id}`, { method: 'DELETE' });
        if (r.ok) table.setData(); else alert(`Ошибка ${r.status}`);
    }

    function showError(msg) {
        errBox.textContent = msg;
        errBox.classList.remove('d-none');
        modalEl.querySelector('.modal-body').scrollTop = 0;
    }
    
})()