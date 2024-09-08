using System.Net;

namespace DnsUpdater.Models
{
	public static class Messages
	{
		public static string ServiceStarted(TimeSpan pollDelay, int settingsCount)
		{
			return $"Service started 👍\n" +
			       $"◦ Poll interval **{pollDelay}**\n" +
			       $"◦ Serving **{settingsCount}** item(s) from settings";
		}
		
		public static string ServiceStopped()
		{
			return "Service stopped 👎";
		}
		
		public static string PrivateIpWarning(IPAddress ip)
		{
			return $"Current IP {ip} is private, skipping update";
		}
		
		public static string SuccessUpdated(string provider, string domain, IPAddress ip)
		{
			return $"**{provider}** — domain {domain} DNS record A updated to {ip}";
		}
		
		public static string WarningNotUpdated(string provider, string domain, string? message)
		{
			return $"**{provider}** — domain {domain} DNS record A not updated \n" +
			       $">{message}";
		}
		
		public static string FailedProcess(string provider, string error)
		{
			return $"**{provider}** — failed to process \n" +
			       $">{error}";
		}
		
		public static string FailedUpdateDomain(string provider, string domain, string error)
		{
			return $"**{provider}** — failed to process domain {domain} \n" +
			       $">{error}";
		}
	}
}