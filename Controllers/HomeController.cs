using Microsoft.AspNetCore.Mvc;
using SoundTradeWebApp.Models; // ��� ErrorViewModel
using System.Diagnostics;

namespace SoundTradeWebApp.Controllers
{
    public class HomeController : Controller
    {
        // ������� ����������� ��� �������, ���� ����� ����� ����������
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        // ������� ��������
        public IActionResult Index()
        {
            // ����� ����� ����� ���������� ������ �� ������� ��������, ���� �����
            return View();
        }

        // �������� ������ (������������ UseExceptionHandler � Program.cs)
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            var errorViewModel = new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier };
            // ����� �������� ����������� ������ �����
            _logger.LogError($"Error Occurred. TraceId: {errorViewModel.RequestId}");
            return View(errorViewModel);
        }

    }
}