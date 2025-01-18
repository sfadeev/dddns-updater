using DnsUpdater.Services;

namespace DnsUpdater.Models
{
	public class AppOptions : IConfigOptions
	{
		public static string SectionName => "Settings";

		public string? BaseUrl { get; init; }
		
		public int MaxUpdatesPerDomain { get; set; } = 10;
		
		public int MaxBackups { get; set; } = 7;
	}
}