using System.Net;

namespace DnsUpdater.Services
{
	public interface IIpResolver
	{
		Task<IPAddress[]> ResolveIpAddress(string hostNameOrAddress, CancellationToken cancellationToken);
	}

	public class DefaultIpResolver : IIpResolver
	{
		public async Task<IPAddress[]> ResolveIpAddress(string hostNameOrAddress, CancellationToken cancellationToken)
		{
			var result = await Dns.GetHostAddressesAsync(hostNameOrAddress, cancellationToken);
			
			return result;
		}
	}
}