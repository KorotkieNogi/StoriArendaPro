// wwwroot/js/site.js
document.addEventListener('DOMContentLoaded', function () {
    // Копирование номера телефона
    document.querySelectorAll('.copy-phone').forEach(btn => {
        btn.addEventListener('click', function (e) {
            e.preventDefault();
            const phoneNumber = this.getAttribute('data-number');
            navigator.clipboard.writeText(phoneNumber)
                .then(() => {
                    const originalText = this.innerHTML;
                    this.innerHTML = '<i class="fas fa-check me-2"></i>Скопировано!';
                    setTimeout(() => {
                        this.innerHTML = originalText;
                    }, 2000);
                })
                .catch(err => {
                    console.error('Ошибка копирования: ', err);
                    alert('Не удалось скопировать номер');
                });
        });
    });

    // Для мобильных - сразу звонить при клике на основной номер
    if (/Android|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent)) {
        document.querySelectorAll('.phone-dropdown > button').forEach(btn => {
            btn.addEventListener('click', function (e) {
                // Если меню уже открыто - не переходить по ссылке
                if (this.getAttribute('aria-expanded') === 'true') return;

                e.preventDefault();
                const telLink = this.closest('.phone-dropdown')
                    .querySelector('[href^="tel:"]');
                if (telLink) {
                    window.location.href = telLink.getAttribute('href');
                }
            });
        });
    }
});



// Для сохранения состояния запоминать открытые разделы в Rent.cshtml
document.addEventListener('DOMContentLoaded', function () {
    // Восстановление состояния аккордеона
    const accordion = document.getElementById('categoriesCollapse');
    const storedState = localStorage.getItem('categoriesAccordionState');

    if (storedState === 'collapsed') {
        new bootstrap.Collapse(accordion, { toggle: false });
    }

    // Сохранение состояния при изменении
    accordion.addEventListener('hidden.bs.collapse', function () {
        localStorage.setItem('categoriesAccordionState', 'collapsed');
    });

    accordion.addEventListener('shown.bs.collapse', function () {
        localStorage.setItem('categoriesAccordionState', 'expanded');
    });
});