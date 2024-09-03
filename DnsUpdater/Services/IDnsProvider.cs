using System.Net;

namespace DnsUpdater.Services
{
	public interface IDnsProvider
	{
		Task<bool> UpdateAsync(DnsProviderSettings settings, string domain, IPAddress ipAddress, CancellationToken cancellationToken);
	}
}