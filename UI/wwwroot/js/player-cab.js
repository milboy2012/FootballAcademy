(() => {
    const $ = id => document.getElementById(id);
    const yearsWord = n => n % 10 === 1 && n % 100 !== 11 ? 'год' : [2, 3, 4].includes(n % 10) && ![12, 13, 14].includes(n % 100) ? 'года' : 'лет';

    fetch('/api/me').then(async r => {
        if (!r.ok) { $('notLinked').classList.remove('d-none'); return; }
        const d = await r.json();
        $('home').classList.remove('d-none');
        $('avatar').textContent = d.firstName[0]; if (d.groupColor) $('avatar').style.background = d.groupColor;
        $('hName').textContent = `${d.firstName} ${d.lastName}`;
        $('hSub').textContent = `${d.age} ${yearsWord(d.age)}` + (d.groupName ? ` · группа ${d.groupName} · тренер ${d.coachName}` : ' · группа пока не назначена');
        $('sPresent').textContent = d.present; $('sTotal').textContent = d.total; $('sStreak').textContent = d.streak;
        $('sPercent').textContent = d.total ? d.percent + '%' : '–';
        $('sPercent').className = `fs-1 fw-bold ${d.percent >= 80 ? 'text-success' : d.percent >= 60 ? 'text-warning' : 'text-danger'}`;

        $('next').innerHTML = d.nextTraining
            ? `<div class="fs-5">${new Date(d.nextTraining).toLocaleDateString('ru-RU', { weekday: 'long', day: 'numeric', month: 'long' })}</div>
               <div class="fs-3 fw-bold">${new Date(d.nextTraining).toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit' })}</div>
               <div class="text-muted">📍 ${d.nextVenue}</div>`
            : '<div class="text-muted">Пока ничего не запланировано</div>';
        $('highlights').innerHTML = d.lastHighlights ? `<div class="fst-italic">«${d.lastHighlights}»</div><small class="text-muted">из заметок к последней тренировке</small>` : '<div class="text-muted">Пока нет заметок — всё впереди!</div>';
    });
})();