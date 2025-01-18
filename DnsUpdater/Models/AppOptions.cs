using DnsUpdater.Services;

namespace DnsUpdater.Models
{
	public class AppOptions : IConfigOptions
	{
		public static string SectionName => "Settings";

		public string? BaseUrl { get; init; }
		
		public string UpdatesFilePath { get; set; } = "./data/updates.json";
		
		public string BackupDirPath { get; set; } = "./data/";
		
		public string BackupFileNamePrefix { get; set; } = "backup";
		
		public int MaxUpdatesPerDomain { get; set; } = 10;
		
		public int MaxBackups { get; set; } = 7;
	}
}