document.addEventListener('DOMContentLoaded', function () {
    // --- Аудиоплеер и эквалайзер (Код в основном сохранен) ---
    const audio = document.getElementById('audio');
    const playPauseButton = document.getElementById('playPause');
    const progressContainer = document.querySelector('.progress-bar'); // Используем класс
    const progress = document.getElementById('progress');
    const volumeControl = document.getElementById('volumeControl'); // Используем ID, добавленный в _Layout
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
        canvas.width = window.innerWidth; // Запасной вариант
    }
    canvas.height = 100; // Высота эквалайзера

    let audioContext;
    let analyser;
    let dataArray;
    let source;
    let isAudioContextInitialized = false; // Флаг для однократной инициализации

    // Функция инициализации AudioContext (вызывается при первом воспроизведении)
    function initAudioContextIfNeeded() {
        if (!isAudioContextInitialized && audioContext === undefined) {
            try {
                audioContext = new (window.AudioContext || window.webkitAudioContext)();
                analyser = audioContext.createAnalyser();
                analyser.fftSize = 128; // Количество полос эквалайзера (степень двойки)
                dataArray = new Uint8Array(analyser.frequencyBinCount); // Массив для данных частот

                source = audioContext.createMediaElementSource(audio); // Создаем источник из <audio>
                source.connect(analyser); // Подключаем анализатор к источнику
                analyser.connect(audioContext.destination); // Подключаем анализатор к выходу звука
                isAudioContextInitialized = true;
                console.log("AudioContext инициализирован.");
                drawEqualizer(); // Начинаем рисовать эквалайзер после инициализации
            } catch (e) {
                console.error("Ошибка инициализации AudioContext:", e);
                // Можно скрыть canvas, если контекст не создался
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
            if (isAudioContextInitialized) requestAnimationFrame(drawEqualizer);
            return;
        }

        requestAnimationFrame(drawEqualizer); // Запрашиваем следующий кадр анимации

        analyser.getByteFrequencyData(dataArray); // Получаем данные частот

        ctx.clearRect(0, 0, canvas.width, canvas.height); // Очищаем canvas

        const barWidth = (canvas.width / dataArray.length) * 1.5; // Ширина столбика
        let barHeight;
        let x = 0; // Позиция столбика по X

        // Рисуем столбики эквалайзера
        for (let i = 0; i < dataArray.length; i++) {
            barHeight = dataArray[i] * 0.4; // Масштабируем высоту (подбирается экспериментально)

            // Задаем цвет столбика (можно сделать градиент или зависящим от высоты)
            const red = barHeight + 50 * (i / dataArray.length);
            const green = 150 * (i / dataArray.length);
            const blue = 50;
            ctx.fillStyle = 'rgb(' + red + ',' + green + ',' + blue + ')';

            ctx.fillRect(x, canvas.height - barHeight, barWidth, barHeight); // Рисуем столбик

            x += barWidth + 1; // Сдвигаем позицию для следующего столбика
        }
    }

    // Обработчик кнопки Play/Pause
    if (playPauseButton && audio) {
        playPauseButton.addEventListener('click', () => {
            initAudioContextIfNeeded(); // Инициализируем AudioContext при первом нажатии Play
            if (audio.paused) {
                audio.play().then(() => {
                    playPauseButton.textContent = '❚❚'; // Пауза
                    if (isAudioContextInitialized) drawEqualizer(); // Начать рисовать эквалайзер
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
            if (isFinite(progressPercent)) { // Проверка на NaN или Infinity
                progress.style.width = progressPercent + '%';
            } else {
                progress.style.width = '0%';
            }
        });
    }

    // Перемотка по клику на progress bar
    if (progressContainer && audio) {
        progressContainer.addEventListener('click', function (event) {
            // Проверяем, есть ли длительность у аудиофайла
            if (!audio.duration || !isFinite(audio.duration)) return;

            // Вычисляем позицию клика относительно progressContainer
            const rect = this.getBoundingClientRect(); // Получаем размеры и позицию элемента
            const clickX = event.clientX - rect.left; // Координата X клика внутри элемента
            const width = this.offsetWidth; // Ширина элемента

            // Вычисляем, на какую долю длительности нужно перемотать
            const durationFraction = clickX / width;

            // Устанавливаем новое время воспроизведения
            audio.currentTime = audio.duration * durationFraction;
        });
    }


    // Регулировка громкости
    if (volumeControl && audio) {
        volumeControl.addEventListener('input', (e) => {
            audio.volume = e.target.value;
        });
    }

    // --- Код для модальных окон и вкладок УДАЛЕН ---
    // Функции openModal, closeModal, openTab и связанные обработчики удалены.

    // --- Код для переключения контента в каталоге УДАЛЕН ---
    // Обработчик для #toggleContent и связанных элементов удален,
    // так как он теперь находится в @section Scripts в Views/Catalog/Index.cshtml

}); // Конец DOMContentLoaded