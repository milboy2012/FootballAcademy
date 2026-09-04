(() => {
    const ROLE_RU = { Admin: 'Администратор', Manager: 'Менеджер', Coach: 'Тренер', Parent: 'Родитель', Player: 'Игрок' };
    const fmtDate = d => d ? new Date(d).toLocaleDateString('ru-RU') : '';
    const json = (url, method, body) => fetch(url, { method, headers: { 'Content-Type': 'application/json' }, body: body ? JSON.stringify(body) : undefined });
    const errorOf = async r => (await r.json().catch(() => null))?.error ?? `Ошибка ${r.status}`;

    const $ = id => document.getElementById(id);
    const userModal = new bootstrap.Modal('#userModal'), managerModal = new bootstrap.Modal('#managerModal'), credsModal = new bootstrap.Modal('#credsModal');

    // ---- роли в фильтр и селект карточки ----
    fetch('/api/users/roles').then(r => r.json()).then(roles => {
        for (const r of roles) { $('fRole').add(new Option(ROLE_RU[r] ?? r, r)); $('umRole').add(new Option(ROLE_RU[r] ?? r, r)); }
        $('fRole').add(new Option(ROLE_RU.Admin, 'Admin'));
    });

    // ---- таблица ----
    const table = new Tabulator('#usersTable', {
        layout: 'fitColumns',
        ajaxURL: '/api/users',
        ajaxParams: () => ({ search: $('fSearch').value, role: $('fRole').value, isActive: $('fActive').value }),
        pagination: true, paginationMode: 'remote', paginationSize: 20, paginationSizeSelector: [10, 20, 50, 100],
        sortMode: 'remote', initialSort: [{ column: 'lastName', dir: 'asc' }],
        placeholder: 'Нет пользователей',
        rowFormatter: row => { if (!row.getData().isActive) row.getElement().classList.add('table-secondary', 'text-muted'); },
        columns: [
            { title: 'Фамилия', field: 'lastName', minWidth: 130 },
            { title: 'Имя', field: 'firstName', minWidth: 120 },
            { title: 'Email', field: 'email', minWidth: 200 },
            { title: 'Телефон', field: 'phone', width: 140, headerSort: false },
            { title: 'Роль', field: 'role', width: 140, headerSort: false, formatter: c => ROLE_RU[c.getValue()] ?? c.getValue() },
            { title: 'Создан', field: 'createdAt', width: 110, formatter: c => fmtDate(c.getValue()) },
            {
                title: 'Статус', field: 'isActive', width: 170, hozAlign: 'center',
                formatter: c => {
                    const d = c.getRow().getData();
                    if (!d.isActive) return '<span class="badge bg-danger">заблокирован</span>';
                    return d.mustChangePassword ? '<span class="badge bg-warning text-dark">временный пароль</span>' : '<span class="badge bg-success">активен</span>';
                }
            },
            {
                title: '', field: 'id', width: 60, headerSort: false, hozAlign: 'center',
                formatter: () => '<button class="btn btn-sm btn-outline-primary"><i class="bi bi-pencil"></i></button>',
                cellClick: (e, cell) => openUser(cell.getRow().getData().id)
            }
        ]
    });
    let t; const refresh = () => { clearTimeout(t); t = setTimeout(() => table.setData(), 300); };
    ['fSearch', 'fRole', 'fActive'].forEach(id => $(id).addEventListener(id === 'fSearch' ? 'input' : 'change', refresh));

    // ---- карточка пользователя ----
    let current = null;
    const msg = (el, text) => { $('umError').classList.add('d-none'); $('umOk').classList.add('d-none'); $(el).textContent = text; $(el).classList.remove('d-none'); };

    async function openUser(id) {
        const r = await fetch(`/api/users/${id}`);
        if (!r.ok) return alert(await errorOf(r));
        current = await r.json();
        const f = $('profileForm');
        f.lastName.value = current.lastName; f.firstName.value = current.firstName; f.phone.value = current.phone ?? '';
        $('umEmail').value = current.email;
        $('umTitle').textContent = `${current.lastName} ${current.firstName}`;
        $('umRole').value = current.role;
        $('umRole').disabled = $('btnRole').disabled = current.role === 'Admin';

        const info = [];
        if (current.childrenCount) info.push(`Детей в академии: ${current.childrenCount}`);
        if (current.groupsCount) info.push(`Групп у тренера: ${current.groupsCount}`);
        if (current.linkedPlayer) info.push(`Ученик: ${current.linkedPlayer}`);
        $('umInfo').innerHTML = info.map(i => `<li>${i}</li>`).join('') || '<li>Нет связанных данных</li>';

        const b = $('btnBlock');
        b.className = `btn btn-sm w-100 mb-2 ${current.isActive ? 'btn-outline-danger' : 'btn-outline-success'}`;
        b.innerHTML = current.isActive ? '<i class="bi bi-lock"></i> Заблокировать (ушёл из академии)' : '<i class="bi bi-unlock"></i> Разблокировать (вернулся)';
        $('umError').classList.add('d-none'); $('umOk').classList.add('d-none');
        userModal.show();
    }

    $('profileForm').addEventListener('submit', async e => {
        e.preventDefault(); const f = e.target;
        const r = await json(`/api/users/${current.id}/profile`, 'PUT', { lastName: f.lastName.value, firstName: f.firstName.value, phone: f.phone.value || null });
        r.ok ? (msg('umOk', 'Профиль сохранён'), table.setData()) : msg('umError', await errorOf(r));
    });

    $('btnRole').addEventListener('click', async () => {
        const role = $('umRole').value;
        if (role === current.role) return;
        if (!confirm(`Сменить роль на «${ROLE_RU[role]}»? Пользователю потребуется войти заново.`)) return;
        const r = await json(`/api/users/${current.id}/role`, 'PATCH', { role });
        if (!r.ok) { $('umRole').value = current.role; return msg('umError', await errorOf(r)); }
        msg('umOk', 'Роль изменена'); table.setData(); openUser(current.id);
    });

    $('btnBlock').addEventListener('click', async () => {
        const blocked = current.isActive;
        if (!confirm(blocked ? 'Заблокировать пользователя? Все его сессии будут завершены.' : 'Разблокировать пользователя?')) return;
        const r = await json(`/api/users/${current.id}/block`, 'PATCH', { blocked, reason: null });
        if (!r.ok) return msg('umError', await errorOf(r));
        table.setData(); openUser(current.id);
    });

    $('btnReset').addEventListener('click', async () => {
        if (!confirm('Сбросить пароль и выдать временный?')) return;
        const r = await fetch(`/api/users/${current.id}/reset-password`, { method: 'POST' });
        if (!r.ok) return msg('umError', await errorOf(r));
        userModal.hide(); showCreds(current.email, (await r.json()).password);
    });

    // ---- новый менеджер ----
    function showCreds(email, password) { $('credEmail').textContent = email; $('credPassword').textContent = password; credsModal.show(); }

    $('btnAddManager').addEventListener('click', () => { const f = $('managerForm'); f.reset(); f.classList.remove('was-validated'); $('mgrError').classList.add('d-none'); managerModal.show(); });
    $('managerForm').addEventListener('submit', async e => {
        e.preventDefault(); const f = e.target;
        if (!f.checkValidity()) return f.classList.add('was-validated');
        const r = await json('/api/users/managers', 'POST', { email: f.email.value, lastName: f.lastName.value, firstName: f.firstName.value, phone: f.phone.value || null });
        if (!r.ok) { $('mgrError').textContent = await errorOf(r); $('mgrError').classList.remove('d-none'); return; }
        const d = await r.json(); managerModal.hide(); table.setData(); showCreds(d.email, d.password);
    });
})();