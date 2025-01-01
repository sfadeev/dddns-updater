using System.Net;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using DnsUpdater.Models;
using DnsUpdater.Services.Jobs;

namespace DnsUpdater.Services.DnsProviders
{
	// https://www.nic.ru/help/api-1390/ - api docs
	// https://www.nic.ru/manager/oauth.cgi - app registration
	public class NicRuDnsProvider(ILogger<NicRuDnsProvider> logger, IHttpClientFactory httpClientFactory) : IDnsProvider
	{
		public async Task<Result> UpdateAsync(DnsProviderSettings settings, string domain, IPAddress ipAddress,
			CancellationToken cancellationToken)
		{
			var httpClient = httpClientFactory.CreateClient();
			
			var client = new NicRuHttpClient(logger, httpClient);
				
			var result = await client.Zones(settings, cancellationToken);

			return result.AsResult();
		}
	}

	public class NicRuHttpClient(ILogger logger, HttpClient httpClient)
	{
		private string? _token;

		private async Task EnsureAuthorized(DnsProviderSettings settings, CancellationToken cancellationToken)
		{
			if (_token == null)
			{
				var result = await RequestApi<AuthResponse>(settings, HttpMethod.Post, "/oauth/token", null, cancellationToken);

				if (result.Success && result.Data?.AccessToken != null)
				{
					_token = result.Data.AccessToken;
				}
				else
				{
					throw new InvalidOperationException("Failed to get authorization token.\n" + result.Error);
				}
			}
		}

		public async Task<Result<ServicesResponse>> Services(DnsProviderSettings settings, CancellationToken cancellationToken)
		{
			await EnsureAuthorized(settings, cancellationToken);

			return await RequestApi<ServicesResponse>(settings, HttpMethod.Get, "/dns-master/services", null, cancellationToken);
		}
		
		public async Task<Result<ZonesResponse>> Zones(DnsProviderSettings settings, CancellationToken cancellationToken)
		{
			await EnsureAuthorized(settings, cancellationToken);

			return await RequestApi<ZonesResponse>(settings, HttpMethod.Get, "/dns-master/zones", null, cancellationToken);
		}

		private async Task<Result<TResult>> RequestApi<TResult>(DnsProviderSettings settings,
			HttpMethod httpMethod, string path, object? data, CancellationToken cancellationToken)
		{
			var uriBuilder = new UriBuilder(Uri.UriSchemeHttps, "api.nic.ru") { Path = path };

			var request = new HttpRequestMessage(httpMethod, uriBuilder.Uri);
			
			if (_token == null)
			{
				request.Headers.Authorization = new BasicAuthenticationHeaderValue(settings.ClientId, settings.ClientSecret);
				
				request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
				{
					{ "username", settings.Username! },
					{ "password", settings.Password! },
					{ "grant_type", "password" },
					{ "scope", ".*" } // ".+:/dns-master/.+"
				});
			}
			else
			{
				request.Headers.Authorization = new BearerAuthenticationHeaderValue(_token);
			}
			
			var response = await httpClient.SendAsync(request, cancellationToken);

			var content = await response.Content.ReadAsStringAsync(cancellationToken);

			if (logger.IsEnabled(LogLevel.Debug))
			{
				logger.LogDebug("{httpMethod} {path} - {statusCode} {statusCodeText}\n{content}",
					httpMethod, path, (int)response.StatusCode, response.StatusCode, content);
			}

			if (response.IsSuccessStatusCode)
			{
				TResult? result = default;
				
				if (response.Content.Headers.ContentLength > 0)
				{
					switch (response.Content.Headers.ContentType?.MediaType)
					{
						case "application/json":
							result = await response.Content.ReadFromJsonAsync<TResult>(cancellationToken);
							break;

						case "text/xml":
							using (var stream = await response.Content.ReadAsStreamAsync(cancellationToken))
							{
								var serializer = new XmlSerializer(typeof(TResult));
								result = (TResult?)serializer.Deserialize(stream);
								break;
							}
					}
				}

				return Result.CreateSuccessResult(result);
			}

			return Result.CreateErrorResult<TResult>(content);
		}

		private class AuthResponse
		{
			[JsonPropertyName("access_token")]
			public string? AccessToken { get; set; }
			
			[JsonPropertyName("token_type")]
			public string? TokenType { get; set; }			
			
			[JsonPropertyName("expires_in")]
			public int? ExpiresIn { get; set; }	
			
			[JsonPropertyName("refresh_token")]
			public string? RefreshToken { get; set; }
			
			[JsonPropertyName("error")]
			public string? Error { get; set; }
		}
		
		public abstract class XmlResponse
		{
			[XmlElement("status")]
			public string? Status { get; set; }
		}
		
		[XmlRoot("response")]
		public class ServicesResponse : XmlResponse
		{
			[XmlElement("data")]
			public ServicesResponseData? Data { get; set; }
		}
		
		public class ServicesResponseData
		{
			[XmlElement("service")]
			public ServicesResponseDataService[]? Services { get; set; }
		}
		
		public class ServicesResponseDataService
		{
			/// <summary>
			/// номер договора
			/// </summary>
			[XmlAttribute("admin")]
			public string? Admin { get; set; }
			
			/// <summary>
			/// допустимое количество доменных зон на тариф, по которому предоставляется услуга
			/// </summary>
			[XmlAttribute("domains-limit")]
			public int DomainsLimit { get; set; }
			
			/// <summary>
			/// сколько доменных зон уже размещено на услуге
			/// </summary>
			[XmlAttribute("domains-num")]
			public int DomainsNum { get; set; }
			
			/// <summary>
			/// идентификатор услуги
			/// </summary>
			[XmlAttribute("name")]
			public string? Name { get; set; }
			
			/// <summary>
			/// допустимое количество ресурсных записей на тариф, по которому предоставляется услуга (для услуг DNS-master)
			/// </summary>
			[XmlAttribute("rr-limit")]
			public int RrLimit { get; set; }
			
			/// <summary>
			/// сколько ресурсных записей уже добавлено (для услуг DNS-master)
			/// </summary>
			[XmlAttribute("rr-num")]
			public int RrNum { get; set; }
			
			/// <summary>
			/// тариф, по которому предоставляется услуга
			/// </summary>
			[XmlAttribute("tariff")]
			public string? Tariff { get; set; }
			
			/// <summary>
			/// плательщик
			/// </summary>
			[XmlAttribute("payer")]
			public string? Payer { get; set; }
			
			/// <summary>
			/// услуга предоставляется
			/// </summary>
			[XmlAttribute("enable")]
			public bool Enable { get; set; }
			
			/// <summary>
			/// услуга DNS-master / услуга Secondary
			/// </summary>
			[XmlAttribute("has-primary")]
			public bool HasPrimary { get; set; }
		}
		
		[XmlRoot("response")]
		public class ZonesResponse : XmlResponse
		{
			[XmlElement("data")]
			public ZonesResponseData? Data { get; set; }
		}
		
		public class ZonesResponseData
		{
			[XmlElement("zone")]
			public ZonesResponseDataZone[]? Zones { get; set; }
		}

		public class ZonesResponseDataZone
		{
			/// <summary>
			/// номер договора
			/// </summary>
			[XmlAttribute("admin")]
			public string? Admin { get; set; }
			
			/// <summary>
			/// числовой идентификатор зоны
			/// </summary>
			[XmlAttribute("id")]
			public int Id { get; set; }
			
			/// <summary>
			/// имя доменной зоны в Punycode
			/// </summary>
			[XmlAttribute("name")]
			public string? Name { get; set; }
			
			/// <summary>
			/// IDN имя доменной зоны
			/// </summary>
			[XmlAttribute("idn-name")]
			public string? IdnName { get; set; }
			
			/// <summary>
			/// идентификатор услуги
			/// </summary>
			[XmlAttribute("service")]
			public string? Service { get; set; }
			
			/// <summary>
			/// в зоне есть невыгруженные на DNS–серверы изменения / в зоне нет невыгруженных на DNS–серверы изменений
			/// </summary>
			[XmlAttribute("has-changes")]
			public bool HasChanges { get; set; }
			
			/// <summary>
			/// зона включена и выгружается на DNS–серверы / зона выключена и не выгружается на DNS–серверы
			/// </summary>
			[XmlAttribute("enable")]
			public bool Enable { get; set; }
			
			/// <summary>
			/// услуга DNS-master / услуга Secondary
			/// </summary>
			[XmlAttribute("has-primary")]
			public bool HasPrimary { get; set; }
		}
	}
}