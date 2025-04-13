using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SoundTradeWebApp.Data;
using SoundTradeWebApp.Models;
using SoundTradeWebApp.Models.ViewModels;
using System.Security.Claims;
using System.IO; // Для MemoryStream

namespace SoundTradeWebApp.Controllers
{
    [Authorize(Roles = "Author")] // Доступ только авторам
    public class AuthorPanelController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuthorPanelController> _logger;
        // IWebHostEnvironment больше не нужен для сохранения файлов

        public AuthorPanelController(ApplicationDbContext context, ILogger<AuthorPanelController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: /AuthorPanel/
        // Отображает список треков текущего автора
        public async Task<IActionResult> Index()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId))
            {
                _logger.LogWarning("AuthorPanel/Index: Не удалось получить ID автора из клеймов.");
                return Unauthorized("Не удалось определить пользователя.");
            }

            // Выбираем сразу в ViewModel
            var authorTrackViewModels = await _context.Tracks
                                           .AsNoTracking()
                                           .Where(t => t.AuthorUserId == userId)
                                           .OrderByDescending(t => t.UploadDate)
                                           .Select(t => new TrackIndexViewModel // <-- Проекция в ViewModel
                                           {
                                               Id = t.Id,
                                               Title = t.Title,
                                               ArtistName = t.ArtistName,
                                               Genre = t.Genre,
                                               UploadDate = t.UploadDate // Используем напрямую
                                           })
                                           .ToListAsync(); // Выполняем запрос и получаем List<TrackIndexViewModel>

            return View(authorTrackViewModels); // Передаем список ViewModel в представление
        }

        // GET: /AuthorPanel/Upload
        // Отображение формы загрузки
        public IActionResult Upload()
        {
            return View();
        }

        // POST: /AuthorPanel/Upload
        // Обработка загрузки трека
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(200 * 1024 * 1024)] // Оставьте или установите адекватный лимит
        public async Task<IActionResult> Upload(UploadTrackViewModel model)
        {
            // Объявляем переменные в начале метода
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int userId = 0; // Инициализируем значением по умолчанию

            // Пытаемся получить int ID
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out userId))
            {
                // userId не удалось получить, используем userIdString (который null или не парсится) в логе
                _logger.LogWarning("AuthorPanel/Upload: Не удалось получить или распознать ID автора из клеймов. userIdString: {UserIdString}", userIdString);
                ModelState.AddModelError("", "Ошибка идентификации пользователя.");
                return View(model); // Возвращаем форму с ошибкой
            }
            // userId теперь содержит корректное значение, если TryParse вернул true

            // Валидация файла
            if (model.AudioFile == null || model.AudioFile.Length == 0)
            {
                ModelState.AddModelError(nameof(model.AudioFile), "Необходимо выбрать аудиофайл.");
            }
            else if (!model.AudioFile.ContentType.StartsWith("audio/"))
            {
                ModelState.AddModelError(nameof(model.AudioFile), "Недопустимый тип файла. Выберите аудиофайл.");
            }
            else if (model.AudioFile.Length > 20 * 1024 * 1024) // Лимит 20MB (пример)
            {
                ModelState.AddModelError(nameof(model.AudioFile), "Файл слишком большой (максимум 20MB).");
            }


            if (ModelState.IsValid)
            {
                byte[]? fileBytes = null;
                string contentType = model.AudioFile!.ContentType;
                string? fileNameForLog = model.AudioFile?.FileName;

                _logger.LogInformation("Attempting to upload track. Title: {TrackTitle}, FileName: {FileName}, AuthorId: {AuthorId}", model.Title, fileNameForLog, userId); // <-- ЛОГ 1

                try
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        _logger.LogInformation("Before CopyToAsync for {FileName}", fileNameForLog); // <-- ЛОГ 2
                        await model.AudioFile.CopyToAsync(memoryStream);
                        _logger.LogInformation("After CopyToAsync, before ToArray() for {FileName}. Stream Length: {Length}", fileNameForLog, memoryStream.Length); // <-- ЛОГ 3

                        // Проверка размера (можно убрать, если есть выше)
                        if (memoryStream.Length > 20 * 1024 * 1024)
                        {
                            _logger.LogWarning("File size limit exceeded for {FileName}. Size: {Length}", fileNameForLog, memoryStream.Length);
                            ModelState.AddModelError(nameof(model.AudioFile), $"Файл превышает допустимый размер ({memoryStream.Length / 1024 / 1024} MB). Максимум 20MB.");
                            return View(model);
                        }
                        fileBytes = memoryStream.ToArray();
                        _logger.LogInformation("After ToArray() for {FileName}. Byte array size: {Length}", fileNameForLog, fileBytes.Length); // <-- ЛОГ 4
                    }

                    var track = new Track
                    {
                        Title = model.Title,
                        ArtistName = model.ArtistName,
                        Genre = model.Genre,
                        VocalType = model.VocalType,
                        Mood = model.Mood,
                        Lyrics = model.Lyrics,
                        AudioFileContent = fileBytes ?? Array.Empty<byte>(),
                        AudioContentType = contentType,
                        UploadDate = DateTime.UtcNow,
                        AuthorUserId = userId // Используем полученный ранее userId
                    };

                    _logger.LogInformation("Before Add Track to context. Track ID (before save): {TrackId}, Title: {TrackTitle}", track.Id, track.Title); // <-- ЛОГ 5
                    _context.Tracks.Add(track);

                    _logger.LogInformation("Before SaveChangesAsync for Track Title: {TrackTitle}", track.Title); // <-- ЛОГ 6
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("After SaveChangesAsync. Track '{TrackTitle}' saved with ID: {TrackId}", track.Title, track.Id); // <-- ЛОГ 7

                    TempData["SuccessMessage"] = $"Трек '{track.Title}' успешно загружен!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    // Используем userIdString в логе ошибки, т.к. он гарантированно доступен
                    _logger.LogError(ex, "КРИТИЧЕСКАЯ ОШИБКА при загрузке трека '{TrackTitle}' автором ID: {UserIdString}. FileName: {FileName}", model.Title, userIdString, fileNameForLog);
                    ModelState.AddModelError("", "Произошла внутренняя ошибка при сохранении трека.");
                }
            }
            else // Если ModelState НЕ IsValid
            {
                // Логируем невалидное состояние модели, используя userIdString
                _logger.LogWarning("Model state is invalid for track upload. Title: {TrackTitle}, User ID String: {UserIdString}", model.Title, userIdString); // <-- ЛОГ 8 (Обновлен)
                                                                                                                                                               // Логируем ошибки валидации для детальной диагностики
                foreach (var state in ModelState)
                {
                    if (state.Value.Errors.Any())
                    {
                        _logger.LogWarning("Validation Error for {Field}: {Errors}", state.Key, string.Join(", ", state.Value.Errors.Select(e => e.ErrorMessage)));
                    }
                }
            }

            // Возвращаем представление с моделью (и ошибками валидации)
            return View(model);
        }
    }
}
