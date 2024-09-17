using System.Net;
using DnsUpdater.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace DnsUpdater.Tests.Services
{
	public class JsonUpdateStorageTests
	{
		[Test]
		public async Task JsonUpdateStorage_Test()
		{
			var domainsCount = 3;
			var updatesCount = 64 * IUpdateStorage.MaxUpdates;

			var cancellationToken = new CancellationTokenSource().Token;
			var storage = new JsonUpdateStorage(NullLogger<JsonUpdateStorage>.Instance);

			await Task.WhenAll(Enumerable.Range(0, updatesCount).Select(x =>
			{
				return Task.Run(() => StoreQueryAction(x), cancellationToken);
			}));
			
			var result = await storage.Query(cancellationToken);
			
			Assert.That(result, Is.Not.Null);
			Assert.That(result.Records, Has.Count.EqualTo(domainsCount));

			for (var d = 0; d < domainsCount; d++)
			{
				Assert.That(result.Records[d].Updates, Has.Count.EqualTo(IUpdateStorage.MaxUpdates));
			}

            return;

			async void StoreQueryAction(int i)
			{
				if (i % 7 == 0)
				{
					var domain = $"domain-{i % domainsCount}.tld";
					var ip = IPAddress.Parse($"127.0.0.{i % 256}");

					var updates = await storage.Store(domain, ip, "dns_provider", true, null, cancellationToken);
						
					Assert.That(updates, Is.Not.Null);
				}
				else
				{
					var updates = await storage.Query(cancellationToken);
						
					Assert.That(updates, Is.Not.Null);
				}
			}
		}
	}
}