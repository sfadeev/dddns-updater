using Microsoft.AspNetCore.Mvc;
using DnsUpdater.Services;

namespace DnsUpdater.Controllers
{
	public class HomeController : Controller
	{
		private readonly ILogger<HomeController> _logger;
		private readonly IUpdateStorage _storage;

		public HomeController(ILogger<HomeController> logger, IUpdateStorage storage)
		{
			_logger = logger;
			_storage = storage;
		}

		public async Task<IActionResult> Index(CancellationToken cancellationToken)
		{
			var updates = await _storage.Query(cancellationToken);
			
			return View(updates);
		}
	}
}