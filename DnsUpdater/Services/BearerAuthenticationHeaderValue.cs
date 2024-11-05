using System.Net.Http.Headers;

namespace DnsUpdater.Services
{
	public class BearerAuthenticationHeaderValue(string? token) : AuthenticationHeaderValue("Bearer", token);
}