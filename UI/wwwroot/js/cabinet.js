(() => {
    const modal = new bootstrap.Modal(document.getElementById('childModal'));
    const form = document.getElementById('childForm');
    const errBox = document.getElementById('formError');

    const showError = m => { errBox.textContent = m; errBox.classList.remove('d-none'); };
    const reset = () => { form.reset(); form.classList.remove('was-validated'); errBox.classList.add('d-none'); };

    //const STATUS = { PendingPayment: ['ожидает оплаты', 'warning'], Active: ['активен', 'success'], Expired: ['истёк', 'secondary'], Frozen: ['заморожен', 'info'], Cancelled: ['отменён', 'danger'] };
    const STATUS = { 0: ['ожидает оплаты', 'warning'], 1: ['активен', 'success'], 2: ['истёк', 'secondary'], 3: ['заморожен', 'info'], 4: ['отменён', 'danger'] };
    const subModal = new bootstrap.Modal('#subModal'); let subPlayer = null, plans = [], chosenPlan = null, pendingId = null;

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

    //Работа с подпиской
    async function openSub(playerId, name) {
        subPlayer = playerId;
        chosenPlan = null;
        //$('subTitle').textContent = `Абонемент — ${name}`;

        var subTitle = document.getElementById('subTitle');
        subTitle.textContent = `Абонемент — ${name}`;
        if (!plans.length)
            plans = await (await fetch('/api/subscriptions/plans')).json();

        const d = await (await fetch(`/api/subscriptions/player/${playerId}`)).json();
        var subStatus = document.getElementById('subStatus');
        subStatus.className = `alert alert-${d.status.isValid ? 'success' : d.status.hasPending ? 'warning' : 'danger'}`;

        var subStatus = document.getElementById('subStatus');
        subStatus.textContent = d.status.text;

        pendingId = d.history.find(h => h.status === 'PendingPayment')?.id ?? null;

        //$('pendingBox').classList.toggle('d-none', !pendingId); 
        //$('chooseBox').classList.toggle('d-none', !!pendingId);
        var penBox = document.getElementById('pendingBox');
        penBox.classList.toggle('d-none', !pendingId);

        var chooseBox = document.getElementById('chooseBox');
        chooseBox.classList.toggle('d-none', !!pendingId);
        var pl = document.getElementById('plans');
        pl.innerHTML = plans.map(p =>
            `<div class="col-md-3 head-card">
                <div class="card plan h-100" data-plan="${p.id}" style="cursor:pointer ">
                    <div class="card-body text-center">
                        <div class="fw-bold">${p.name}</div>
                        <div class="fs-4">${p.price.toLocaleString('ru-RU')} ₽</div>
                        <small class="text-muted">${p.type === 'Visits' ? `${p.visits} занятий, действуют ${p.visitsValidDays} дн.` : 'безлимит на период'}</small>
                        ${p.description ? `<div class="small mt-1">${p.description}</div>` : ''}
                    </div>
                </div>
            </div>`)
            .join('');

        // document.querySelectorAll('.plan').forEach(c => c.addEventListener('click', () => {
        //     document.querySelectorAll('.plan').forEach(x => x.classList.remove('border-primary', 'border-2'));
        //     c.classList.add('border-primary', 'border-2');
        //     chosenPlan = c.dataset.plan;

        //     var btnRequest = document.getElementById('btnRequest');
        //     btnRequest.disabled = false;
        // }));

        pl.addEventListener('click', e=> {
            var card = e.target.closest('.plan');
            if(!card) return;

            pl.querySelectorAll('.plan.selected').forEach(x=>x.classList.remove('selected'));
            card.classList.add('selected');
            chosenPlan = card.dataset.plan;
            document.getElementById('btnRequest').disabled = false;
        });

        // $('subFrom').value = ''; 
        // $('subComment').value = ''; 
        // $('btnRequest').disabled = true; 
        var subForm = document.getElementById('subFrom');
        var subComment = document.getElementById('subComment');
        var btnRequest = document.getElementById('btnRequest');
        subForm.value = '';
        subComment.value = '';
        btnRequest.disabled = true;

        //$('subErr').classList.add('d-none');
        var subErr = document.getElementById('subErr');
        subErr.classList.add('d-none');
        var subHistory = document.getElementById('subHistory');
        subHistory.innerHTML = d.history.map(h => {
            const [t, c] = STATUS[h.status];
            return `<tr>
                        <td>${h.planName}</td>
                        <td>${new Date(h.from).toLocaleDateString('ru-RU')} – ${new Date(h.to).toLocaleDateString('ru-RU')}</td>
                        <td>${h.trainingsLimit == null ? '∞' : `${h.trainingsUsed}/${h.trainingsLimit}`}</td><td>${h.price.toLocaleString('ru-RU')} ₽</td>
                        <td><span class="badge bg-${c}">${t}</span>${h.managerComment ? `<br><small class="text-muted">${h.managerComment}</small>` : ''}</td>
                    </tr>`;
        }).join('') || '<tr><td colspan="5" class="text-muted">Пока нет</td></tr>';

        subModal.show();
    }

    document.querySelectorAll('[data-sub]').forEach(b => b.addEventListener('click', () => openSub(b.dataset.sub, b.dataset.name)));
    var btnRequest = document.getElementById('btnRequest');
    btnRequest.addEventListener('click', async () => {
        const r = await post('/api/subscriptions/request', {
            playerId: subPlayer,
            planId: chosenPlan,
            startFrom: document.getElementById('subFrom').value || null,
            comment: document.getElementById('subComment').value || null
        });
        if (!r.ok) {

            document.getElementById('subErr').textContent = (await r.json().catch(() => null))?.error ?? 'Ошибка';
            document.getElementById('subErr').classList.remove('d-none');
            return;
        }
        location.reload();
    });
    var btnCancelReq = document.getElementById('btnCancelReq');
    btnCancelReq.addEventListener('click', async e => {
        e.preventDefault();
        if (!confirm('Отменить заявку?')) return;
        const r = await fetch(`/api/subscriptions/request/${pendingId}`, { method: 'DELETE' }); r.ok ? location.reload() : fail(r);
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