// Mevcut JavaScript kodunuz (eğer varsa)

// Tema geçiş mantığı
document.addEventListener('DOMContentLoaded', () => {
    const themeToggleBtn = document.getElementById('theme-toggle');
    const body = document.body;

    // Kullanıcının tercihini veya sistem tercihini kontrol et
    const currentTheme = localStorage.getItem('theme');
    const systemPrefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;

    // Sayfa yüklendiğinde doğru temayı ayarla
    if (currentTheme) {
        body.classList.add(currentTheme);
    } else if (systemPrefersDark) {
        body.classList.add('dark-theme');
        localStorage.setItem('theme', 'dark-theme');
    } else {
        body.classList.add('light-theme');
        localStorage.setItem('theme', 'light-theme');
    }

    // Düğmeye tıklandığında temayı değiştir ve ikonu güncelle
    if (themeToggleBtn) {
        // Butonun başlangıç metin ve ikonunu ayarla (Şimdi sadece ikon olacak)
        // Eğer body dark-theme ise, ışıklandırma tuşu (güneş ikonu) göstermeliyiz, değilse karanlık tuşu (ay ikonu)
        if (body.classList.contains('dark-theme')) {
            themeToggleBtn.innerHTML = '<i class="fas fa-sun"></i>'; // Şu an karanlık, "Açık Tema"ya geçmek için güneşi göster
        } else {
            themeToggleBtn.innerHTML = '<i class="fas fa-moon"></i>'; // Şu an açık, "Karanlık Tema"ya geçmek için ayı göster
        }

        themeToggleBtn.addEventListener('click', () => {
            if (body.classList.contains('light-theme')) {
                body.classList.remove('light-theme');
                body.classList.add('dark-theme');
                localStorage.setItem('theme', 'dark-theme');
                themeToggleBtn.innerHTML = '<i class="fas fa-sun"></i>'; // Karanlığa geçildi, "Açık Tema"ya geçmek için güneşi göster
            } else {
                body.classList.remove('dark-theme');
                body.classList.add('light-theme');
                localStorage.setItem('theme', 'light-theme');
                themeToggleBtn.innerHTML = '<i class="fas fa-moon"></i>'; // Açığa geçildi, "Karanlık Tema"ya geçmek için ayı göster
            }
        });
    }
});