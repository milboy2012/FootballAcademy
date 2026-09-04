(() => {
    const fmtDate = d => d ? new Date(d).toLocaleDateString('ru-RU') : '';
    const fail = async r => alert((await r.json().catch(() => null))?.error ?? `Ошибка ${r.status}`);

    const table = new Tabulator('#coachesTable', {
        layout: 'fitColumns',
        ajaxURL: '/api/coaches',
        placeholder: 'Тренеров пока нет',
        columns: [
            { title: 'ФИО', field: 'fullName', minWidth: 180 },
            { title: 'Email', field: 'email', minWidth: 200 },
            { title: 'Принят', field: 'hiredAt', width: 120, formatter: c => fmtDate(c.getValue()) },
            { title: 'Квалификация', field: 'qualification' },
            { title: 'Групп', field: 'groupsCount', width: 90, hozAlign: 'center' },
            {
                title: 'Статус', field: 'isActive', width: 170, hozAlign: 'center',
                formatter: c => {
                    const r = c.getRow().getData();
                    if (!r.isActive) return '<span class="badge bg-secondary">отключён</span>';
                    return r.mustChangePassword
                        ? '<span class="badge bg-warning text-dark">ожидает первого входа</span>'
                        : '<span class="badge bg-success">активен</span>';
                }
            },
            {
                title: '', field: 'id', width: 60, headerSort: false, hozAlign: 'center',
                formatter: () => '<button class="btn btn-sm btn-outline-secondary" title="Сбросить пароль"><i class="bi bi-key"></i></button>',
                cellClick: async (e, cell) => {
                    const r = cell.getRow().getData();
                    if (!confirm(`Сбросить пароль тренеру ${r.fullName}? Текущие сессии будут завершены.`)) return;
                    const resp = await fetch(`/api/coaches/${r.id}/reset-password`, { method: 'POST' });
                    if (!resp.ok) return fail(resp);
                    showCreds(r.email, (await resp.json()).password);
                }
            }
        ]
    });

    const createModal = new bootstrap.Modal('#createModal');
    const credsModal = new bootstrap.Modal('#credsModal');
    const form = document.getElementById('createForm');
    const err = document.getElementById('createError');

    function showCreds(email, password) {
        document.getElementById('credEmail').value = email;
        document.getElementById('credPassword').value = password;
        credsModal.show();
    }

    document.getElementById('btnAdd').addEventListener('click', () => {
        form.reset(); form.classList.remove('was-validated'); err.classList.add('d-none');
        createModal.show();
    });

    form.addEventListener('submit', async e => {
        e.preventDefault();
        if (!form.checkValidity()) { form.classList.add('was-validated'); return; }
        const r = await fetch('/api/coaches', {
            method: 'POST', headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ email: form.email.value, firstName: form.firstName.value || null, lastName: form.lastName.value || null })
        });
        if (!r.ok) {
            err.textContent = (await r.json().catch(() => null))?.error ?? `Ошибка ${r.status}`;
            err.classList.remove('d-none'); return;
        }
        const data = await r.json();
        createModal.hide();
        table.setData();
        showCreds(data.email, data.temporaryPassword);
    });

    document.getElementById('btnCopy').addEventListener('click', () =>
        navigator.clipboard.writeText(document.getElementById('credPassword').value));
})();