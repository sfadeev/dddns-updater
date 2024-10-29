using System.Net;
using DnsUpdater.Models;

namespace DnsUpdater.Services
{
	public interface IIpProvider
	{
		Task<Result<IPAddress>> GetCurrentIpAddress(CancellationToken cancellationToken);
	}
}