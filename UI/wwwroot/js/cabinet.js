(() => {
    const modal = new bootstrap.Modal(document.getElementById('childModal'));
    const form = document.getElementById('childForm');
    const errBox = document.getElementById('formError');

    const showError = m => { errBox.textContent = m; errBox.classList.remove('d-none'); };
    const reset = () => { form.reset(); form.classList.remove('was-validated'); errBox.classList.add('d-none'); };

    document.getElementById('btnAdd').addEventListener('click', () => {
        reset();
        form.id.value = '';
        modal.show();
    });

    document.querySelectorAll('[data-edit]').forEach(btn =>
        btn.addEventListener('click', async () => {
            const r = await fetch(`/api/players/${btn.dataset.edit}`);
            if (!r.ok) { alert('Не удалось загрузить данные'); return; }
            const p = await r.json();
            reset();
            form.id.value = p.id;
            form.lastName.value = p.lastName;
            form.firstName.value = p.firstName;
            form.birthDate.value = p.birthDate;
            form.medicalCertificateUntil.value = p.medicalCertificateUntil ?? '';
            modal.show();
        }));

    form.addEventListener('submit', async e => {
        e.preventDefault();
        if (!form.checkValidity()) { form.classList.add('was-validated'); return; }

        const id = form.id.value;
        const dto = {
            lastName: form.lastName.value,
            firstName: form.firstName.value,
            birthDate: form.birthDate.value,
            medicalCertificateUntil: form.medicalCertificateUntil.value || null,
            note: form.note.value || null,
            parentId: '00000000-0000-0000-0000-000000000000', // сервер подставит текущего родителя
            isActive: true
        };

        const r = await fetch(id ? `/api/players/${id}` : '/api/players', {
            method: id ? 'PUT' : 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(dto)
        });

        if (r.ok) { location.reload(); return; }
        const body = await r.json().catch(() => null);
        showError(body?.error ?? body?.title ?? `Ошибка ${r.status}`);
    });


    const creds = new bootstrap.Modal(document.getElementById('credsModal'));
    const showCreds = (login, password) => {
        document.getElementById('credLogin').textContent = login;
        document.getElementById('credPassword').textContent = password;
        creds.show();
    };
    const post = (url, body, method = 'POST') => fetch(url, {
        method, headers: { 'Content-Type': 'application/json' }, body: body ? JSON.stringify(body) : undefined
    });
    const fail = async r => alert((await r.json().catch(() => null))?.error ?? `Ошибка ${r.status}`);

    document.querySelectorAll('[data-create-account]').forEach(b => b.addEventListener('click', async () => {
        const r = await post(`/api/players/${b.dataset.createAccount}/account`, { password: null });
        r.ok ? (({ login, password }) => showCreds(login, password))(await r.json()) : fail(r);
    }));

    document.querySelectorAll('[data-reset]').forEach(b => b.addEventListener('click', async () => {
        if (!confirm('Сбросить пароль ребёнка?')) return;
        const r = await post(`/api/players/${b.dataset.reset}/account/reset-password`);
        r.ok ? showCreds(b.closest('dd').querySelector('code').textContent, (await r.json()).password) : fail(r);
    }));

    document.querySelectorAll('[data-toggle]').forEach(b => b.addEventListener('click', async () => {
        const r = await post(`/api/players/${b.dataset.toggle}/account/active?value=${b.dataset.active}`, null, 'PATCH');
        r.ok ? location.reload() : fail(r);
    }));
})();