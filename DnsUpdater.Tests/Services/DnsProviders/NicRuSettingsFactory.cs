using DnsUpdater.Services.Jobs;

namespace DnsUpdater.Tests.Services.DnsProviders
{
	public abstract class NicRuSettingsFactory
	{
		public static DnsProviderSettings CreateTestSettings()
		{
			return new DnsProviderSettings
			{
				Provider = "nicru",
				ClientId = Environment.GetEnvironmentVariable("NicRu.ClientId"),
				ClientSecret = Environment.GetEnvironmentVariable("NicRu.ClientSecret"),
				Username = Environment.GetEnvironmentVariable("NicRu.Username"),
				Password = Environment.GetEnvironmentVariable("NicRu.Password")
			};
		}
	}
}