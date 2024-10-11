namespace DnsUpdater.Services
{
	public interface IHealthcheckService
	{
		public Task<bool> Success(CancellationToken cancellationToken = default);
		
		public Task<bool> Start(CancellationToken cancellationToken = default);
		
		public Task<bool> Failure(string message, CancellationToken cancellationToken = default);
		
		public Task<bool> Log(string message, CancellationToken cancellationToken = default);
	}

	public class HealthcheckIoSettings
	{
		public string? Url { get; set; }
	}
	
	public class HealthcheckIoService(ILogger<HealthcheckIoService> logger,
		IConfiguration configuration, IHttpClientFactory httpClientFactory) : IHealthcheckService
	{
		public async Task<bool> Success(CancellationToken cancellationToken = default)
		{
			return await Ping(string.Empty, null, cancellationToken);
		}

		public async Task<bool> Start(CancellationToken cancellationToken = default)
		{
			return await Ping("/start", null, cancellationToken);
		}

		public async Task<bool> Failure(string message, CancellationToken cancellationToken = default)
		{
			return await Ping("/fail", message, cancellationToken);
		}

		public async Task<bool> Log(string message, CancellationToken cancellationToken = default)
		{
			return await Ping("/log", message, cancellationToken);
		}

		private async Task<bool> Ping(string method, string? message, CancellationToken cancellationToken)
		{
			var settings = configuration.GetSection("HealthcheckIo").Get<HealthcheckIoSettings>();
			
			if (settings?.Url == null)
			{
				logger.LogDebug("healthcheck.io is not configured, ping not sent.");
					
				return false;
			}

			try
			{
				using (var client = httpClientFactory.CreateClient())
				{
					var uriBuilder = new UriBuilder($"{settings.Url}{method}");

					var content = new StringContent(message ?? string.Empty);
					
					var response = await client.PostAsync(uriBuilder.Uri, content, cancellationToken);

					response.EnsureSuccessStatusCode();
					
					return true;
				}
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Failed to ping {message}", method);
				
				return false;
			}
		}
	}
}