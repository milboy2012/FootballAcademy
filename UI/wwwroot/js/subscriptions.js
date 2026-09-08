(() => {
    const $ = id => document.getElementById(id);
    const json = (u, m, b) => fetch(u, { method: m, headers: { 'Content-Type': 'application/json' }, body: b ? JSON.stringify(b) : undefined });
    const errorOf = async r => (await r.json().catch(() => null))?.error ?? `Ошибка ${r.status}`;
    const fmtD = d => d ? new Date(d).toLocaleDateString('ru-RU') : '';
    const money = v => (v ?? 0).toLocaleString('ru-RU') + ' ₽';
    const STATUS = {
        0: ['ожидает оплаты', 'warning text-dark'],
        1: ['активен', 'success'],
        2: ['истёк', 'secondary'],
        3: ['заморожен', 'info text-dark'],
        4: ['отменён', 'danger']
    };
    const statusValue = () => document.querySelector('[name=st]:checked').value;

    fetch('/api/subscriptions').then(r => r.json()).then(s => {
        console.log(s);
    });

    async function loadSummary() {
        const s = await (await fetch('/api/subscriptions/summary')).json();
        $('cPending').textContent = s.pending; $('cActive').textContent = s.active;
        $('cExpiring').textContent = s.expiringSoon;
        $('cRevenue').textContent = money(s.monthRevenue);
    }

    const table = new Tabulator('#subsTable', {
        layout: 'fitColumns', placeholder: 'Нет записей', pagination: true, paginationSize: 25,
        ajaxURL: '/api/subscriptions',
        ajaxParams: () => { const st = statusValue(); return { status: st && st !== 'other' ? st : '', search: $('fSearch').value }; },
        ajaxResponse: (url, params, resp) => statusValue() === 'other'
            ? resp.data.filter(r => !['PendingPayment', 'Active'].includes(r.status)) : resp.data,
        rowFormatter: row => {
            const d = row.getData();
            if (d.status === 'Active' && d.daysLeft <= 7) row.getElement().classList.add('table-warning');
            if (d.status === 'Active' && d.trainingsLeft === 0) row.getElement().classList.add('table-danger');
        },
        columns: [
            { title: 'Ребёнок', field: 'playerName', minWidth: 160 },
            { title: 'План', field: 'planName', width: 130 },
            { title: 'Период', field: 'from', width: 190, formatter: c => { const d = c.getRow().getData(); return `${fmtD(d.from)} – ${fmtD(d.to)}` + (d.status === 'Active' ? `<br><small class="text-muted">${d.daysLeft >= 0 ? 'осталось ' + d.daysLeft + ' дн.' : 'просрочен'}</small>` : ''); } },
            { title: 'Занятия', field: 'trainingsUsed', width: 90, hozAlign: 'center', formatter: c => { const d = c.getRow().getData(); return d.trainingsLimit == null ? '∞' : `${d.trainingsUsed}/${d.trainingsLimit}`; } },
            { title: 'Сумма', field: 'price', width: 110, hozAlign: 'right', formatter: c => { const d = c.getRow().getData(); return money(d.price) + (d.paid && d.paid !== d.price ? `<br><small class="text-muted">оплачено ${money(d.paid)}</small>` : ''); } },
            { title: 'Статус', field: 'status', width: 140, hozAlign: 'center', formatter: c => { const [t, cls] = STATUS[c.getValue()]; return `<span class="badge bg-${cls}">${t}</span>`; } },
            //{ title: 'Статус', field: 'status', width: 140, hozAlign: 'center' },
            { title: 'Комментарии', field: 'parentComment', minWidth: 180, headerSort: false, formatter: c => { const d = c.getRow().getData(); return [d.parentComment ? `<small>👪 ${d.parentComment}</small>` : '', d.managerComment ? `<small class="text-muted">🛠 ${d.managerComment}</small>` : ''].filter(Boolean).join('<br>'); } },
            { title: 'Создано', field: 'createdAt', width: 100, formatter: c => fmtD(c.getValue()) },
            {
                title: '', field: 'id', width: 150, headerSort: false, hozAlign: 'center',
                formatter: c => {
                    const d = c.getRow().getData();
                    switch (d.status) {
                        case 0: return '<button class="btn btn-sm btn-success" data-act="confirm"><i class="bi bi-check-lg"></i> Оплата</button>';
                        case 1: return '<button class="btn btn-sm btn-outline-info me-1" data-act="freeze" title="Заморозить"><i class="bi bi-snow"></i></button><button class="btn btn-sm btn-outline-primary me-1" data-act="extend" title="Продлить"><i class="bi bi-calendar-plus"></i></button><button class="btn btn-sm btn-outline-danger" data-act="cancel" title="Отменить"><i class="bi bi-x-lg"></i></button>';
                        case 2: return '<button class="btn btn-sm btn-outline-success me-1" data-act="activate" title="Разморозить"><i class="bi bi-play"></i></button><button class="btn btn-sm btn-outline-primary" data-act="extend" title="Продлить"><i class="bi bi-calendar-plus"></i></button>';
                        case 3: return '<button class="btn btn-sm btn-outline-primary" data-act="extend" title="Продлить"><i class="bi bi-calendar-plus"></i></button>';
                        default: return '';
                    }
                },
                cellClick: (e, cell) => {
                    const act = e.target.closest('button')?.dataset.act; if (!act) return;
                    const d = cell.getRow().getData();
                    ({
                        confirm: openConfirm, extend: openExtend, freeze: () => setStatus(d, 'Frozen', 'Заморозить абонемент? Тренер не сможет отмечать присутствие.'),
                        activate: () => setStatus(d, 'Active', 'Разморозить абонемент?'), cancel: () => setStatus(d, 'Cancelled', 'Отменить абонемент? Действие необратимо.')
                    })[act](d);
                }
            }
        ]
    });
    const refresh = () => {
        table.setData();
        loadSummary();
    };

    let t;
    $('fSearch').addEventListener('input', () => {
        clearTimeout(t);
        t = setTimeout(refresh, 300);
    });

    document.querySelectorAll('[name=st]').forEach(r => r.addEventListener('change', refresh));

    // ---- подтверждение / отклонение ----
    const confirmModal = new bootstrap.Modal('#confirmModal'), cForm = $('confirmForm');
    let current = null;

    function openConfirm(d) {
        current = d;
        console.log(current);
        cForm.reset();
        cForm.classList.remove('was-validated');
        $('confirmErr').classList.add('d-none');
        cForm.amount.value = d.price;

        $('confirmInfo').innerHTML = `<dt class="col-4">Ребёнок</dt><dd class="col-8">${d.playerName}</dd>
            <dt class="col-4">План</dt><dd class="col-8">${d.planName}${d.trainingsLimit ? ` (${d.trainingsLimit} занятий)` : ''}</dd>
            <dt class="col-4">Период</dt><dd class="col-8">${fmtD(d.from)} – ${fmtD(d.to)}</dd>
            <dt class="col-4">Заявка</dt><dd class="col-8">${fmtD(d.createdAt)}${d.parentComment ? `<br><i>${d.parentComment}</i>` : ''}</dd>`;
        confirmModal.show();
    }

    cForm.addEventListener('submit', async e => {
        e.preventDefault();

        if (!cForm.checkValidity()) return cForm.classList.add('was-validated');

        var str = '/api/subscriptions/' + current.id + '/confirm';
        console.log(str);

        const r = await json('/api/subscriptions/' + current.id + '/confirm', 'POST', {
            amount: +cForm.amount.value,
            method: +cForm.method.value,
            comment: cForm.comment.value || null
        });

        if (!r.ok) {
            $('confirmErr').textContent = await errorOf(r);
            $('confirmErr').classList.remove('d-none');
            return;
        }
        confirmModal.hide();
        refresh();
    });

    $('btnReject').addEventListener('click', async () => {
        const reason = prompt('Причина отклонения (будет отправлена родителю):', 'Оплата не поступила');
        if (reason === null) return;
        const r = await json(`/api/subscriptions/${current.id}/reject`, 'POST', { reason: reason || null });
        if (!r.ok) return alert(await errorOf(r));
        confirmModal.hide(); refresh();
    });

    // ---- смена статуса ----
    async function setStatus(d, value, question) {
        if (!confirm(question)) return;
        const r = await fetch(`/api/subscriptions/${d.id}/status?value=${value}`, { method: 'PATCH' });
        r.ok ? refresh() : alert(await errorOf(r));
    }

    // ---- продление ----
    const extendModal = new bootstrap.Modal('#extendModal'), eForm = $('extendForm');
    function openExtend(d) {
        current = d;
        eForm.reset();
        eForm.classList.remove('was-validated');
        $('extendErr').classList.add('d-none');
        $('extendInfo').textContent = `${d.playerName}, ${d.planName}. Сейчас до ${fmtD(d.to)}`;

        const min = new Date(d.to); min.setDate(min.getDate() + 1);
        eForm.newTo.min = min.toISOString().slice(0, 10);
        eForm.newTo.value = eForm.newTo.min;
        extendModal.show();
    }
    eForm.addEventListener('submit', async e => {
        e.preventDefault();
        if (!eForm.checkValidity()) return eForm.classList.add('was-validated');

        const r = await json(`/api/subscriptions/${current.id}/extend`, 'POST', {
            newTo: eForm.newTo.value,
            comment: eForm.comment.value || null
        });

        if (!r.ok) {
            $('extendErr').textContent = await errorOf(r);
            $('extendErr').classList.remove('d-none');
            return;
        }
        extendModal.hide();
        refresh();
    });

    loadSummary();
})();