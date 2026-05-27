using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using iOStore.Models;

namespace iOStore.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error(int? id)
        {
            var vm = new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                StatusCode = id ?? 500
            };

            vm.Message = vm.StatusCode switch
            {
                404 => "No encontramos la página que buscás.",
                403 => "No tenés permiso para acceder a este recurso.",
                _   => "Ocurrió un error inesperado. Por favor intentá nuevamente."
            };

            if (vm.StatusCode >= 500)
                _logger.LogError("Error HTTP {StatusCode} — RequestId: {RequestId}", vm.StatusCode, vm.RequestId);

            return View(vm);
        }
    }
}
