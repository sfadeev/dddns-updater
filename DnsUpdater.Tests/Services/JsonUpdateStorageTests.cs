using System.Net;
using DnsUpdater.Models;
using DnsUpdater.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DnsUpdater.Tests.Services
{
	public class JsonUpdateStorageTests
	{
		[Test]
		public async Task JsonUpdateStorage_QueryAndUpdate_ShouldBeConsistent()
		{
			// arrange
			const int domainsCount = 3;
			var options = new AppOptions { MaxUpdatesPerDomain = 5 };
			var updatesCount = 64 * options.MaxUpdatesPerDomain;
			
			var cancellationToken = new CancellationTokenSource().Token;
			var appOptions = Options.Create(options);
			var storage = new JsonUpdateStorage(NullLogger<JsonUpdateStorage>.Instance, appOptions);

			// act
			await Task.WhenAll(Enumerable.Range(0, updatesCount).Select(x =>
			{
				return Task.Run(() => StoreQueryAction(x), cancellationToken);
			}));
			
			var result = await storage.Query(cancellationToken);
			
			// assert
			Assert.That(result, Is.Not.Null);
			
			var records = result.Records.Where(x => x.Domain.StartsWith("domain-") && x.Domain.EndsWith(".tld")).ToList();
			Assert.That(records, Has.Count.EqualTo(domainsCount));

			for (var d = 0; d < domainsCount; d++)
			{
				Assert.That(records[d].Updates, Has.Count.EqualTo(options.MaxUpdatesPerDomain));
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

		[Test]
		public async Task JsonUpdateStorage_Backup_ShouldWork()
		{
			// arrange
			var cancellationToken = new CancellationTokenSource().Token;
			var appOptions = Options.Create(new AppOptions { MaxBackups = 2 });
			var storage = new JsonUpdateStorage(NullLogger<JsonUpdateStorage>.Instance, appOptions);
			
			// act
			var result = await storage.Backup(BackupMode.Force, cancellationToken);
			
			// assert
			Assert.That(result, Is.Not.Null);
			Assert.That(result.Success, Is.True);
			Assert.That(result.Data, Is.Not.Null);
		}
	}
}