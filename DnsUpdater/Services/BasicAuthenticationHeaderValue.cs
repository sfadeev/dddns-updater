using System.Net.Http.Headers;
using System.Text;

namespace DnsUpdater.Services
{
	public class BasicAuthenticationHeaderValue(string? username, string? password)
		: AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}")));
}