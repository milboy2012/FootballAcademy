(() => {
    const $ = id => document.getElementById(id);
    const form = $('profileForm'), f = n => form.elements[n];
    const ROLE = { Admin: 'Администратор', Manager: 'Менеджер', Coach: 'Тренер', Parent: 'Родитель', Player: 'Игрок' };
    const json = (u, m, b) => fetch(u, { method: m, headers: { 'Content-Type': 'application/json' }, body: b ? JSON.stringify(b) : undefined });
    const errorOf = async r => (await r.json().catch(() => null))?.error ?? `Ошибка ${r.status}`;
    const show = (id, text) => { ['err', 'ok'].forEach(x => $(x).classList.add('d-none')); if (id) { if (text) $(id).textContent = text; $(id).classList.remove('d-none'); } };
    let role;

    function setAvatar(url, name) {
        $('avatar').classList.toggle('d-none', !url); $('avatarPlaceholder').classList.toggle('d-none', !!url);
        if (url) $('avatar').src = url; else $('avatarPlaceholder').textContent = (name || '?')[0].toUpperCase();
    }

    async function load() {
        const p = await (await fetch('/api/profile')).json();
        role = p.role;
        $('pName').textContent = `${p.lastName} ${p.firstName}`; $('pRole').textContent = ROLE[p.role] ?? p.role;
        $('pSince').textContent = `в академии с ${new Date(p.createdAt).toLocaleDateString('ru-RU')}`;
        $('pEmail').value = p.role === 'Player' ? p.userName : p.email;
        setAvatar(p.avatarUrl, p.firstName);

        f('lastName').value = p.lastName; f('firstName').value = p.firstName; f('middleName').value = p.middleName ?? '';
        f('phone').value = p.phone ?? ''; f('birthDate').value = p.birthDate ?? ''; f('city').value = p.city ?? ''; f('about').value = p.about ?? '';
        f('notifyByEmail').checked = p.notifyByEmail;

        if (p.coach) {
            $('coachBlock').classList.remove('d-none');
            f('coach.qualification').value = p.coach.qualification ?? ''; f('coach.experienceYears').value = p.coach.experienceYears ?? '';
            f('coach.bio').value = p.coach.bio ?? ''; f('coach.achievements').value = p.coach.achievements ?? '';
            $('coachRo').textContent = `Принят: ${p.coach.hiredAt ? new Date(p.coach.hiredAt).toLocaleDateString('ru-RU') : '—'} · Группы: ${p.coach.groups.join(', ') || 'нет'}`;
        }
        if (p.parent) {
            $('parentBlock').classList.remove('d-none');
            for (const k of ['secondPhone', 'address', 'emergencyContactName', 'emergencyContactPhone', 'notes']) f('parent.' + k).value = p.parent[k] ?? '';
        }
        if (p.role === 'Player') {
            $('playerBlock').classList.remove('d-none');
            ['lastName', 'firstName', 'middleName', 'birthDate'].forEach(n => f(n).disabled = true);
            $('notifyByEmail').closest('.form-check').classList.add('d-none');
            const pl = p.player ?? {};
            $('playerRo').innerHTML = `<dt class="col-4">Группа</dt><dd class="col-8">${pl.groupName ?? 'не назначена'}</dd>
                <dt class="col-4">Тренер</dt><dd class="col-8">${pl.coachName ?? '—'}</dd><dt class="col-4">Родитель</dt><dd class="col-8">${pl.parentName ?? '—'}</dd>
                <dt class="col-4">Медсправка до</dt><dd class="col-8">${pl.medicalUntil ? new Date(pl.medicalUntil).toLocaleDateString('ru-RU') : 'нет'}</dd>`;
        }
    }

    form.addEventListener('submit', async e => {
        e.preventDefault(); if (!form.checkValidity()) return form.classList.add('was-validated');
        const dto = {
            lastName: f('lastName').value, firstName: f('firstName').value, middleName: f('middleName').value || null,
            phone: f('phone').value || null, birthDate: f('birthDate').value || null, city: f('city').value || null,
            about: f('about').value || null, notifyByEmail: f('notifyByEmail').checked,
            coach: role === 'Coach' ? { qualification: f('coach.qualification').value || null, experienceYears: f('coach.experienceYears').value ? +f('coach.experienceYears').value : null, bio: f('coach.bio').value || null, achievements: f('coach.achievements').value || null } : null,
            parent: role === 'Parent' ? Object.fromEntries(['secondPhone', 'address', 'emergencyContactName', 'emergencyContactPhone', 'notes'].map(k => [k, f('parent.' + k).value || null])) : null
        };
        const r = await json('/api/profile', 'PUT', dto);
        if (!r.ok) return show('err', await errorOf(r));
        show('ok'); $('pName').textContent = `${dto.lastName} ${dto.firstName}`;
    });

    $('avatarFile').addEventListener('change', async e => {
        const file = e.target.files[0]; if (!file) return;
        const fd = new FormData(); fd.append('file', file);
        const r = await fetch('/api/profile/avatar', { method: 'POST', body: fd });
        if (!r.ok) return alert(await errorOf(r));
        setAvatar((await r.json()).url, f('firstName').value); e.target.value = '';
    });
    $('btnRemoveAvatar').addEventListener('click', async () => {
        const r = await fetch('/api/profile/avatar', { method: 'DELETE' });
        r.ok ? setAvatar(null, f('firstName').value) : alert(await errorOf(r));
    });

    $('pwdForm').addEventListener('submit', async e => {
        e.preventDefault(); const pf = e.target; $('pwdErr').classList.add('d-none'); $('pwdOk').classList.add('d-none');
        if (pf.next.value !== pf.confirm.value) { $('pwdErr').textContent = 'Пароли не совпадают'; $('pwdErr').classList.remove('d-none'); return; }
        const r = await json('/api/profile/password', 'POST', { currentPassword: pf.current.value, newPassword: pf.next.value });
        if (!r.ok) { $('pwdErr').textContent = await errorOf(r); $('pwdErr').classList.remove('d-none'); return; }
        pf.reset(); $('pwdOk').classList.remove('d-none');
    });

    load();
})();