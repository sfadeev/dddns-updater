using System.Reflection;
using System.Text.Json;
using DnsUpdater.Services.Jobs;

namespace DnsUpdater.Services
{
	public interface IMessageSender
	{
		Task<bool> Send(string message, MessageType messageType, CancellationToken cancellationToken);
	}
	
	public class Message
	{
		public string[]? Urls { get; set; }
		
		public string? Title { get; set; }
		
		public string? Body { get; set; }
		
		public string? Type { get; set; }
		
		public string? Format { get; set; }
	}
	
	public enum MessageType
	{
		Info,
		Success,
		Warning,
		Failure
	}

	public class AppriseSettings
	{
		public string? ServiceUrl { get; set; }
		
		public string[]? NotifyUrls { get; set; }
	}
	
	public class AppriseMessageSender(ILogger<AppriseMessageSender> logger, 
		IConfiguration configuration, IHttpClientFactory httpClientFactory) : IMessageSender
	{
		private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
		
		public async Task<bool> Send(string message, MessageType messageType, CancellationToken cancellationToken)
		{
			try
			{
				var appSettings = configuration.GetSection("Settings").Get<DnsUpdaterSettings>();
				var settings = configuration.GetSection("Apprise").Get<AppriseSettings>();
				
				if (settings?.ServiceUrl == null)
				{
					logger.LogDebug("Apprise notifications is not configured, message not sent.\n{message}", message);
					
					return false;
				}
				
				var icon = messageType switch
				{
					MessageType.Success => "🟢",
					MessageType.Warning => "🟡",
					MessageType.Failure => "🔴",
					_ => string.Empty
				};

				var title = icon + Assembly.GetEntryAssembly()?.GetName().Name + " @ " + Environment.MachineName
				            + (appSettings?.BaseUrl != null ? " — " + appSettings?.BaseUrl : string.Empty);
				
				var bodyJson = JsonSerializer.Serialize(new Message
				{
					Urls = settings.NotifyUrls,
					Title = title,
					Body = message,
					Type = messageType.ToString().ToLower(),
					Format = "markdown"
				}, JsonOptions);
				
				var content = new StringContent(bodyJson, System.Text.Encoding.UTF8,  "application/json");
			
				var httpClient = httpClientFactory.CreateClient();

				var result = await httpClient.PostAsync(settings.ServiceUrl, content, cancellationToken);

				result.EnsureSuccessStatusCode();
			
				return true;
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Failed to send message:\n{message}", message);
				
				return false;
			}
		}
	}
}