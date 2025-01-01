using System.Net;
using DnsUpdater.Models;
using DnsUpdater.Services.Jobs;

namespace DnsUpdater.Services.DnsProviders
{
	public class NicRuDynamicDnsProvider(ILogger<NicRuDynamicDnsProvider> logger, IHttpClientFactory httpClientFactory) : IDnsProvider
	{
		public async Task<Result> UpdateAsync(DnsProviderSettings settings, string domain, IPAddress ipAddress, CancellationToken cancellationToken)
		{
			var httpClient = httpClientFactory.CreateClient();
			
			var client = new NicRuDynamicDnsHttpClient(logger, httpClient);
				
			var result = await client.UpdateAsync(settings, domain, ipAddress, cancellationToken);

			return result.AsResult();
		}
	}

	// https://www.nic.ru/help/dinamicheskij-dns-dlya-razrabotchikov_4391.html - Динамический DNS для разработчиков
	public class NicRuDynamicDnsHttpClient(ILogger logger, HttpClient httpClient)
	{
		public async Task<Result> UpdateAsync(DnsProviderSettings settings, string domain, IPAddress ipAddress, CancellationToken cancellationToken)
		{
			var uriBuilder = new UriBuilder(Uri.UriSchemeHttps, "api.nic.ru")
			{
				Path = "/dyndns/update",
				Query = new QueryString()
					.Add("hostname", domain)
					.Add("myip", ipAddress.ToString())
					.ToString()
			};
			
			var request = new HttpRequestMessage(HttpMethod.Get, uriBuilder.Uri);
			
			request.Headers.Authorization = new BasicAuthenticationHeaderValue(settings.Username, settings.Password);

			var response = await httpClient.SendAsync(request, cancellationToken);

			var content = await response.Content.ReadAsStringAsync(cancellationToken);

			if (logger.IsEnabled(LogLevel.Debug))
			{
				logger.LogDebug("{httpMethod} {path} - {statusCode} {statusCodeText}\n{content}",
					uriBuilder.Scheme, uriBuilder.Path, (int)response.StatusCode, response.StatusCode, content);
			}

			if (response.IsSuccessStatusCode && content.StartsWith("good"))
			{
				return Result.CreateSuccessResult();
			}
			
			return Result.CreateErrorResult(content);
		}
	}
}