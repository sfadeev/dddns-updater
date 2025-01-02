using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DnsUpdater.Models;
using DnsUpdater.Services.Jobs;

namespace DnsUpdater.Services.DnsProviders
{
	// https://www.reg.ru/reseller/api2doc
	public class RegRuDnsProvider(RegRuHttpClient client) : IDnsProvider
	{
		public async Task<Result> UpdateAsync(DnsProviderSettings settings, string domain, IPAddress ipAddress, CancellationToken cancellationToken)
		{
			var result = await client.ServiceNop(settings, domain, cancellationToken);
			// var result = await client.ZoneNop(settings, domain, cancellationToken);
			
			return result.AsResult();
		}
	}

	public class RegRuHttpClient(ILogger<RegRuHttpClient> logger, IHttpClientFactory httpClientFactory)
	{
		private static readonly JsonSerializerOptions JsonSerializerOptions = new()
		{
			DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
		};

		public async Task<Result<RegRuResponse<NopResponse>>> ServiceNop(DnsProviderSettings settings, string domain, CancellationToken cancellationToken)
		{
			var path = $"api/regru2/service/nop";
			
			return await RequestApi<RegRuResponse<NopResponse>>(settings, domain, path, null, cancellationToken);
		}
		
		public async Task<Result<RegRuResponse<NopResponse>>> ZoneNop(DnsProviderSettings settings, string domain, CancellationToken cancellationToken)
		{
			var path = $"api/regru2/zone/nop";
			
			return await RequestApi<RegRuResponse<NopResponse>>(settings, domain, path, null, cancellationToken);
		}

		private async Task<Result<TResult>> RequestApi<TResult>(DnsProviderSettings settings, string domain,
			string path, object? data, CancellationToken cancellationToken)
		{
			var client = httpClientFactory.CreateClient();

			var uriBuilder = new UriBuilder(Uri.UriSchemeHttps, "api.reg.ru") { Path = path };

			var request = new HttpRequestMessage(HttpMethod.Post, uriBuilder.Uri);
			
			request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
			
			if (data == null)
			{
				request.Content = new FormUrlEncodedContent([
					new ("username", settings.Username!),
					new ("password", settings.Password!),
					new ("domain_name", domain)
				]);
			}
			else
			{
				var jsonData = JsonSerializer.Serialize(data, JsonSerializerOptions);
				
				request.Content = new StringContent(jsonData, Encoding.UTF8, "application/json");
			}

			var response = await client.SendAsync(request, cancellationToken);
			
			var content = await response.Content.ReadAsStringAsync(cancellationToken);
			
			if (logger.IsEnabled(LogLevel.Debug))
			{
				logger.LogDebug("{path} - {statusCode} {statusCodeText}\n{content}",
					path, (int)response.StatusCode, response.StatusCode, content);
			}

			if (response.IsSuccessStatusCode)
			{
				var result = (response.Content.Headers.ContentLength > 0) 
					? await response.Content.ReadFromJsonAsync<TResult>(cancellationToken)
					: default;
			
				return Result.CreateSuccessResult(result);
			}

			return Result.CreateErrorResult<TResult>(content);
		}
	}

	public class RegRuResponse<TAnswer>
	{
		[JsonPropertyName("result")]
		public string? Result { get; set; }
		
		[JsonPropertyName("error_code")]
		public string? ErrorCode { get; set; }
		
		[JsonPropertyName("error_text")]
		public string? ErrorText { get; set; }
		
		[JsonPropertyName("answer")]
		public TAnswer? Answer { get; set; }
	}
	
	public class NopResponse
	{
		[JsonPropertyName("domains")]
		public RegRuDomainInfo[]? Domains { get; set; }
	}

	public class RegRuDomainInfo
	{
		[JsonPropertyName("dname")]
		public string? DName { get; set; }

		[JsonPropertyName("result")]
		public string? Result { get; set; }
		
		[JsonPropertyName("service_id")]
		public string? ServiceId { get; set; }
	}
}