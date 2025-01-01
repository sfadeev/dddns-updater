using System.Net;
using DnsUpdater.Services.DnsProviders;
using Microsoft.Extensions.Logging.Abstractions;

namespace DnsUpdater.Tests.Services.DnsProviders
{
	public class NicRuDynamicDnsHttpClientTest
	{
		[Test, Ignore("Integration test")]
		public async Task UpdateApi_ForRealAccount_ShouldUpdateIp()
		{
			// arrange
			var cancellationToken = new CancellationTokenSource().Token;
			var client = new NicRuDynamicDnsHttpClient(NullLogger<NicRuDnsProvider>.Instance, new HttpClient());
			var settings = NicRuSettingsFactory.CreateTestSettings();
			var domain = Environment.GetEnvironmentVariable("NicRu.Domain")!;
			var ip = IPAddress.Parse("31.177.76.7");
			
			// act
			var result = await client.UpdateAsync(settings, domain, ip, cancellationToken);

			// assert
			Assert.That(result, Is.Not.Null);
			Assert.That(result.Success, Is.True);
		}
	}
}