console.log("Скрипт script.js загружен!");

document.addEventListener('DOMContentLoaded', function () {
    // --- Аудиоплеер и эквалайзер (Код в основном сохранен) ---
    const audio = document.getElementById('audio');
    const playPauseButton = document.getElementById('playPause');
    const progressContainer = document.querySelector('.progress-bar');
    const progress = document.getElementById('progress');
    const volumeControl = document.getElementById('volumeControl');
    const canvas = document.getElementById('equalizerCanvas');

    // Проверяем наличие всех элементов перед началом работы
    if (!audio || !playPauseButton || !progressContainer || !progress || !volumeControl || !canvas) {
        console.error("Ошибка: Не все элементы аудиоплеера найдены.");
        // Можно остановить выполнение скрипта плеера, если что-то критичное отсутствует
        // return;
    }

    const ctx = canvas.getContext('2d');
    // Устанавливаем размер canvas (можно оставить или адаптировать)
    const footerElement = document.querySelector('footer');
    if (footerElement) {
        canvas.width = footerElement.offsetWidth;
    } else {
        canvas.width = window.innerWidth;  // Запасной вариант
    }
    canvas.height = 100; // Высота эквалайзера

    let audioContext;
    let analyser;
    let dataArray;
    let source;
    let isAudioContextInitialized = false; // Флаг для однократной инициализации
    let animationFrameId; // Для хранения ID requestAnimationFrame

    // Функция инициализации AudioContext
    function initAudioContext() {
        if (!isAudioContextInitialized && audioContext === undefined) {
            try {
                audioContext = new (window.AudioContext || window.webkitAudioContext)();
                analyser = audioContext.createAnalyser();
                analyser.fftSize = 128;
                dataArray = new Uint8Array(analyser.frequencyBinCount);

                source = audioContext.createMediaElementSource(audio);
                source.connect(analyser);
                analyser.connect(audioContext.destination);
                isAudioContextInitialized = true;
                console.log("AudioContext инициализирован.");
            } catch (e) {
                console.error("Ошибка инициализации AudioContext:", e);
                if (canvas) canvas.style.display = 'none';
            }
        }
    }

    // Функция рисования эквалайзера
    function drawEqualizer() {
        // Продолжаем рисовать только если контекст создан и плеер играет
        if (!isAudioContextInitialized || audio.paused) {
            // Очищаем canvas, если плеер остановлен или не инициализирован
            if (ctx) {
                ctx.clearRect(0, 0, canvas.width, canvas.height);
            }
            // Запрашиваем следующий кадр, чтобы очистка сработала, если плеер остановили
            if (isAudioContextInitialized) {
                cancelAnimationFrame(animationFrameId);
                animationFrameId = requestAnimationFrame(drawEqualizer);
            }
            return;
        }

        cancelAnimationFrame(animationFrameId);
        animationFrameId = requestAnimationFrame(drawEqualizer);

        analyser.getByteFrequencyData(dataArray);

        ctx.clearRect(0, 0, canvas.width, canvas.height);

        const barWidth = (canvas.width / dataArray.length) * 1.5;
        let barHeight;
        let x = 0;

        for (let i = 0; i < dataArray.length; i++) {
            barHeight = dataArray[i] * 0.4;

            const red = barHeight + 50 * (i / dataArray.length);
            const green = 150 * (i / dataArray.length);
            const blue = 50;
            ctx.fillStyle = 'rgb(' + red + ',' + green + ',' + blue + ')';
            ctx.fillRect(x, canvas.height - barHeight, barWidth, barHeight);

            x += barWidth + 1;
        }
    }

    // Инициализация AudioContext при загрузке метаданных аудио
    if (audio) {
        audio.addEventListener('loadedmetadata', () => {
            initAudioContext();
        });

        // Автоматическое воспроизведение и запуск эквалайзера
        audio.addEventListener('play', () => {
            if (isAudioContextInitialized) {
                drawEqualizer();
            }
        });
    }

    // Обработчик кнопки Play/Pause
    if (playPauseButton && audio) {
        playPauseButton.addEventListener('click', () => {
            if (audio.paused) {
                audio.play().then(() => {
                    playPauseButton.textContent = '❚❚'; // Пауза
                }).catch(error => console.error("Ошибка воспроизведения:", error));
            } else {
                audio.pause();
                playPauseButton.textContent = '▶'; // Плей
            }
        });
    }

    // Обновление полосы прогресса
    if (audio && progress) {
        audio.addEventListener('timeupdate', () => {
            const progressPercent = (audio.currentTime / audio.duration) * 100;
            if (isFinite(progressPercent)) {
                progress.style.width = progressPercent + '%';
            } else {
                progress.style.width = '0%';
            }
        });
    }

    // Перемотка по клику на progress bar
    if (progressContainer && audio) {
        progressContainer.addEventListener('click', function (event) {
            if (!audio.duration || !isFinite(audio.duration)) return;

            const rect = this.getBoundingClientRect();
            const clickX = event.clientX - rect.left;
            const width = this.offsetWidth;

            const durationFraction = clickX / width;

            audio.currentTime = audio.duration * durationFraction;
        });
    }

    // Регулировка громкости
    if (volumeControl && audio) {
        volumeControl.addEventListener('input', (e) => {
            audio.volume = e.target.value;
        });
    }

    // Обработчики событий для отслеживания ухода со страницы
    // Заменяем unload и beforeunload на visibilitychange и pagehide
    document.addEventListener('visibilitychange', function () {
        if (document.hidden) {
            // Страница стала невидимой (например, переключение вкладок)
            // Здесь можно добавить код для сохранения состояния аудио, если это необходимо
            // Например, audio.pause();
            console.log("Страница стала невидимой");
        } else {
            // Страница снова видима
            // Здесь можно добавить код для восстановления состояния, если это необходимо
            console.log("Страница снова видима");
        }
    });

    window.addEventListener('pagehide', function (event) {
        if (event.persisted) {
            // Страница кэшируется браузером (back/forward cache)
            console.log("Страница будет взята из кэша");
        } else {
            // Страница выгружается полностью
            // Здесь можно добавить код для финализации работы с аудио, если это необходимо
            // Например, audio.pause();
            console.log("Страница выгружается");
        }
    });
}); // Конец DOMContentLoaded