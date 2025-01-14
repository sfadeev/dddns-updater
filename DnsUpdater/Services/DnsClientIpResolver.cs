using System.Net;
using DnsClient;

namespace DnsUpdater.Services
{
	public class DnsClientIpResolver : IIpResolver
	{
		private readonly LookupClient _client = new();
		
		public async Task<IPAddress[]> ResolveIpAddress(string hostNameOrAddress, CancellationToken cancellationToken)
		{
			var question = new DnsQuestion(hostNameOrAddress, QueryType.A);

			var options = new DnsQueryAndServerOptions
			{
				ThrowDnsErrors = true
			};

			var response = await _client.QueryAsync(question, options, cancellationToken);

			var result = response.Answers.ARecords().Select(aRecord => aRecord.Address).ToArray();
			
			return result;
		}
	}
}