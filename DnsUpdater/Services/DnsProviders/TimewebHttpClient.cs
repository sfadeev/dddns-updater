using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DnsUpdater.Models;
using DnsUpdater.Services.Jobs;

namespace DnsUpdater.Services.DnsProviders
{
	// https://timeweb.com/ru/docs/publichnyj-api-timeweb/metody-api-dlya-virtualnogo-hostinga/#poluchenie-resursnyh-zapisej-domena
	public class TimewebHttpClient(ILogger<TimewebHttpClient> logger, IHttpClientFactory httpClientFactory)
	{
		private static readonly JsonSerializerOptions JsonSerializerOptions = new()
		{
			DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
		};
		
		private string? _token;
		
		private async Task EnsureAuthorized(DnsProviderSettings settings, CancellationToken cancellationToken)
		{
			if (_token == null)
			{
				var result = await RequestApi<AuthResponse>(settings, HttpMethod.Post, "/v1.2/access", null, cancellationToken);

				if (result.Success && result.Data?.Token != null)
				{
					_token = result.Data.Token;
				}
				else
				{
					throw new InvalidOperationException("Failed to get authorization token.\n" + result.Error);
				}
			}
		}
		
		public async Task<Result<GetUserRecordsResponse>> GetUserRecords(DnsProviderSettings settings, string domain, CancellationToken cancellationToken)
		{
			await EnsureAuthorized(settings, cancellationToken);
			
			var path = $"/v1.2/accounts/{settings.Username}/domains/{domain}/user-records?limit=100";
			
			return await RequestApi<GetUserRecordsResponse>(settings, HttpMethod.Get, path, null, cancellationToken);
		}
		
		public async Task<Result<AddUserRecordsResponse>> AddUserRecord(DnsProviderSettings settings, string domain, AddUserRecordRequest record, CancellationToken cancellationToken)
		{
			await EnsureAuthorized(settings, cancellationToken);
			
			var path = $"/v1.2/accounts/{settings.Username}/domains/{domain}/user-records/";
			
			return await RequestApi<AddUserRecordsResponse>(settings, HttpMethod.Post, path, record, cancellationToken);
		}
		
		public async Task<Result<DeleteUserRecordsResponse>> DeleteUserRecord(DnsProviderSettings settings, string domain, int recordId, CancellationToken cancellationToken)
		{
			await EnsureAuthorized(settings, cancellationToken);
			
			var path = $"/v1.2/accounts/{settings.Username}/domains/{domain}/user-records/{recordId}/";
			
			return await RequestApi<DeleteUserRecordsResponse>(settings, HttpMethod.Delete, path, null, cancellationToken);
		}
		
		private async Task<Result<TResult>> RequestApi<TResult>(DnsProviderSettings settings,
			HttpMethod httpMethod, string path, object? data, CancellationToken cancellationToken)
		{
			var client = httpClientFactory.CreateClient();
			
			var uriBuilder = new UriBuilder(Uri.UriSchemeHttps, "api.timeweb.ru") { Path = path };

			var request = new HttpRequestMessage(httpMethod, uriBuilder.Uri);

			request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
			request.Headers.Add("x-app-key", settings.ConfigurationSection!["appkey"]);

			request.Headers.Authorization = _token == null
				? new BasicAuthenticationHeaderValue(settings.Username, settings.Password)
				: new BearerAuthenticationHeaderValue(_token);

			if (data != null)
			{
				var jsonData = JsonSerializer.Serialize(data, JsonSerializerOptions);

				request.Content = new StringContent(jsonData, Encoding.UTF8, "application/json");
			}

			var response = await client.SendAsync(request, cancellationToken);

			var content = await response.Content.ReadAsStringAsync(cancellationToken);

			if (logger.IsEnabled(LogLevel.Debug))
			{
				logger.LogDebug("{httpMethod} {path} - {statusCode} {statusCodeText}\n{content}",
					httpMethod, path, (int)response.StatusCode, response.StatusCode, content);
			}

			if (response.IsSuccessStatusCode)
			{
				var result = response.Content.Headers.ContentLength > 0
					? await response.Content.ReadFromJsonAsync<TResult>(cancellationToken)
					: default;

				return Result.CreateSuccessResult(result);
			}

			return Result.CreateErrorResult<TResult>(content);
		}
	}
	
	public class AuthResponse
	{
		[JsonPropertyName("expires_in")]
		public int? ExpiresIn { get; set; }

		[JsonPropertyName("generation")]
		public int? Generation { get; set; }

		[JsonPropertyName("is_manual")]
		public bool? IsManual { get; set; }

		[JsonPropertyName("password")]
		public string? Password { get; set; }

		[JsonPropertyName("registrationDate")]
		public string? RegistrationDate { get; set; }

		[JsonPropertyName("timestamp")]
		public int? Timestamp { get; set; }

		[JsonPropertyName("token")]
		public string? Token { get; set; }

		[JsonPropertyName("token_type")]
		public string? TokenType { get; set; }

		[JsonPropertyName("user")]
		public string? User { get; set; }

		[JsonPropertyName("user_type")]
		public string? UserType { get; set; }
	}
	
	public class GetUserRecordsResponse
	{
		[JsonPropertyName("subdomain")]
        public string? Subdomain { get; set; }

        [JsonPropertyName("advanced_settings")]
        public string? AdvancedSettings { get; set; }

        [JsonPropertyName("v_primary")]
        public string? VPrimary { get; set; }

        [JsonPropertyName("our_domain")]
        public string? OurDomain { get; set; }

        [JsonPropertyName("v_date")]
        public DateTime? VDate { get; set; }

        [JsonPropertyName("ssl_check_key")]
        public string? SslCheckKey { get; set; }

        [JsonPropertyName("records")]
        public UserRecord[]? Records { get; set; }

        [JsonPropertyName("whois_expiration")]
        public string? WhoisExpiration { get; set; }

        [JsonPropertyName("idn_name")]
        public string? IdnName { get; set; }

        [JsonPropertyName("customer_id")]
        public string? CustomerId { get; set; }

        [JsonPropertyName("server")]
        public string? Server { get; set; }

        [JsonPropertyName("serial")]
        public int? Serial { get; set; }

        [JsonPropertyName("custom_log")]
        public string? CustomLog { get; set; }

        [JsonPropertyName("is_premium")]
        public int? IsPremium { get; set; }

        [JsonPropertyName("ddos")]
        public string? Ddos { get; set; }

        [JsonPropertyName("expiration")]
        public string? Expiration { get; set; }

        [JsonPropertyName("awstats")]
        public string? Awstats { get; set; }

        [JsonPropertyName("protection")]
        public string? Protection { get; set; }

        [JsonPropertyName("ssl_check_file")]
        public string? SslCheckFile { get; set; }

        [JsonPropertyName("expiration_last_update")]
        public string? ExpirationLastUpdate { get; set; }

        [JsonPropertyName("webserver")]
        public string? Webserver { get; set; }

        [JsonPropertyName("tld_name")]
        public string? TldName { get; set; }

        [JsonPropertyName("bind_to_constructor_site")]
        public int? BindToConstructorSite { get; set; }

        [JsonPropertyName("id")]
        public int? Id { get; set; }

        [JsonPropertyName("ip_id")]
        public int? IpId { get; set; }

        [JsonPropertyName("certificate_id")]
        public string? CertificateId { get; set; }

        [JsonPropertyName("site_id")]
        public int? SiteId { get; set; }

        [JsonPropertyName("is_favorite")]
        public int? IsFavorite { get; set; }

        [JsonPropertyName("reversed_fqdn")]
        public string? ReversedFqdn { get; set; }

        [JsonPropertyName("fqdn")]
        public string? Fqdn { get; set; }

        [JsonPropertyName("vds_id")]
        public int? VdsId { get; set; }

        [JsonPropertyName("error_log")]
        public string? ErrorLog { get; set; }

        [JsonPropertyName("is_system")]
        public string? IsSystem { get; set; }

        [JsonPropertyName("blocked")]
        public string? Blocked { get; set; }
	}
	
	public class AddUserRecordsResponse
	{
	}
	
	public class DeleteUserRecordsResponse
	{
	}

	public class UserRecord
	{
		[JsonPropertyName("fqdn")]
		public string? Fqdn { get; set; }

		[JsonPropertyName("id")]
		public int? Id { get; set; }

		[JsonPropertyName("num")]
		public string? Num { get; set; }

		[JsonPropertyName("reversed_fqdn")]
		public string? ReversedFqdn { get; set; }

		[JsonPropertyName("srv_weight")]
		public int? SrvWeight { get; set; }

		[JsonPropertyName("ttl")]
		public int? Ttl { get; set; }

		[JsonPropertyName("type")]
		public string? Type { get; set; }

		[JsonPropertyName("value")]
		public string? Value { get; set; }
	}
	
	public class AddUserRecordRequest
	{
		[JsonPropertyName("type")]
		public string? Type { get; set; }

		[JsonPropertyName("data")]
		public UserRecord? Data { get; set; }
	}
}