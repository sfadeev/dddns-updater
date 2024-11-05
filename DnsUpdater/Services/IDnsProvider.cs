using System.Net;
using DnsUpdater.Models;

namespace DnsUpdater.Services
{
	public interface IDnsProvider
	{
		Task<Result> UpdateAsync(DnsProviderSettings settings, string domain, IPAddress ipAddress, CancellationToken cancellationToken);
	}
}