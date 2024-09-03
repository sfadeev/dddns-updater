using System.Net;

namespace DnsUpdater.Services
{
	public interface IIpProvider
	{
		Task<IPAddress> GetCurrentIpAddress(CancellationToken cancellationToken);
	}
}