using System.Net;

namespace DnsUpdater.Services
{
	public interface IDnsProvider
	{
		Task<DnsUpdateResult> UpdateAsync(DnsProviderSettings settings, string domain, IPAddress ipAddress, CancellationToken cancellationToken);
	}

	public class DnsUpdateResult
	{
		public bool Success { get; set; }
		
		public string? Message { get; set; }
	}
}