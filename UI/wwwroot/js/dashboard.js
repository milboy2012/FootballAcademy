(() => {
    const $ = id => document.getElementById(id);
    const money = v => v.toLocaleString('ru-RU') + ' Br';
    const P = ['#0d6efd', '#198754', '#dc3545', '#fd7e14', '#6f42c1', '#20c997', '#ffc107', '#0dcaf0'];
    const bar = (el, labels, data, colors, opts = {}) => new Chart($(el), { type: 'bar', data: { labels, datasets: [{ data, backgroundColor: colors }] }, options: { plugins: { legend: { display: false } }, ...opts } });

    fetch('/api/dashboard').then(r => r.json()).then(d => {
        $('updated').textContent = 'обновлено ' + new Date().toLocaleTimeString('ru-RU');
        $('alerts').innerHTML = d.alerts.map(a => `<a href="${a.link ?? '#'}" class="alert alert-${a.kind} py-2 mb-2 d-block text-decoration-none">${a.text}</a>`).join('');

        $('k-activePlayers').textContent = d.activePlayers; $('k-activePlayers-sub').textContent = d.newPlayersMonth ? `+${d.newPlayersMonth} за месяц` : '';
        $('k-groups').textContent = d.groups; $('k-coaches').textContent = d.coaches; $('k-coaches-sub').textContent = `родителей: ${d.parents}`;
        $('k-attendancePercentMonth').textContent = d.attendancePercentMonth + '%'; $('k-attendancePercentMonth-sub').textContent = `${d.trainingsCompletedMonth} проведено · ${d.trainingsCancelledMonth} отменено`;
        $('k-revenueMonth').textContent = money(d.revenueMonth);
        const diff = d.revenuePrevMonth ? Math.round((d.revenueMonth - d.revenuePrevMonth) * 100 / d.revenuePrevMonth) : null;
        $('k-revenueMonth-sub').innerHTML = diff == null ? '' : `<span class="${diff >= 0 ? 'text-success' : 'text-danger'}">${diff >= 0 ? '▲' : '▼'} ${Math.abs(diff)}% к прошлому месяцу</span>`;
        $('k-pendingSubscriptions').textContent = d.pendingSubscriptions; $('k-pendingSubscriptions-sub').textContent = `без абонемента: ${d.playersWithoutSubscription}`;
        $('k-expiringWeek').textContent = d.expiringWeek; $('k-expiringWeek-sub').textContent = `активных: ${d.activeSubscriptions}`;
        $('k-trainingsWeek').textContent = d.trainingsWeek; $('k-trainingsWeek-sub').textContent = d.medicalExpired ? `⚠ без медсправки: ${d.medicalExpired}` : '';

        bar('chRevenue', d.revenueByMonth.map(p => p.label), d.revenueByMonth.map(p => p.value), '#198754', { scales: { y: { ticks: { callback: v => v.toLocaleString('ru-RU') } } } });
        new Chart($('chAtt'), { type: 'line', data: { labels: d.attendanceByWeek.map(p => p.label), datasets: [{ data: d.attendanceByWeek.map(p => p.value), borderColor: '#0d6efd', backgroundColor: 'rgba(13,110,253,.15)', fill: true, tension: .3 }] }, options: { plugins: { legend: { display: false } }, scales: { y: { min: 0, max: 100 } } } });
        new Chart($('chFill'), {
            type: 'bar', data: {
                labels: d.groupFill.map(g => g.name), datasets: [
                    { label: 'Игроков', data: d.groupFill.map(g => g.value), backgroundColor: d.groupFill.map(g => g.color ?? '#3788d8') },
                    { label: 'Свободно', data: d.groupFill.map(g => Math.max(0, g.max - g.value)), backgroundColor: '#e9ecef' }]
            },
            options: { indexAxis: 'y', scales: { x: { stacked: true }, y: { stacked: true } }, plugins: { legend: { position: 'bottom' } } }
        });
        bar('chAttGroup', d.attendanceByGroup.map(g => g.name), d.attendanceByGroup.map(g => g.value), d.attendanceByGroup.map(g => g.value >= 80 ? '#198754' : g.value >= 60 ? '#ffc107' : '#dc3545'), { scales: { y: { min: 0, max: 100 } } });
        new Chart($('chReasons'), { type: 'doughnut', data: { labels: d.absenceReasons.map(r => r.name), datasets: [{ data: d.absenceReasons.map(r => r.value), backgroundColor: P }] }, options: { plugins: { legend: { position: 'bottom' } } } });
        bar('chVenues', d.venueLoad.map(v => v.name), d.venueLoad.map(v => Math.round(v.value)), '#6f42c1');
        new Chart($('chSkills'), { type: 'radar', data: { labels: d.skillAverages.map(s => s.name), datasets: [{ data: d.skillAverages.map(s => s.value), borderColor: '#fd7e14', backgroundColor: 'rgba(253,126,20,.2)' }] }, options: { plugins: { legend: { display: false } }, scales: { r: { min: 0, max: 10 } } } });
    });
})();