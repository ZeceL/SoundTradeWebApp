using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace SoundTradeWebApp.Models.ViewModels
{
    public class UploadTrackViewModel
    {
        [Required(ErrorMessage = "Введите название трека")]
        [StringLength(200)]
        [Display(Name = "Название трека")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Введите имя исполнителя/автора")]
        [StringLength(150)]
        [Display(Name = "Исполнитель / Автор")]
        public string ArtistName { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Жанр")]
        public string? Genre { get; set; }

        [StringLength(50)]
        [Display(Name = "Тип вокала")]
        public string? VocalType { get; set; }

        [StringLength(50)]
        [Display(Name = "Настроение")]
        public string? Mood { get; set; }

        [DataType(DataType.MultilineText)]
        [Display(Name = "Текст песни (если есть)")]
        public string? Lyrics { get; set; }

        [Required(ErrorMessage = "Выберите аудиофайл для загрузки")]
        [Display(Name = "Аудиофайл (.mp3, .wav, .ogg)")]
        public IFormFile? AudioFile { get; set; } // Тип IFormFile для приема файла
    }
}