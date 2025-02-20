using System.Net;
using System.Text.Json.Nodes;
using DnsUpdater.Models;
using DnsUpdater.Services.Jobs;

namespace DnsUpdater.Services.DnsProviders
{
	// https://beget.com/en/kb/api/dns-administration-functions
	public class BegetDnsProvider(ILogger<BegetDnsProvider> logger, IHttpClientFactory httpClientFactory) : IDnsProvider
	{
		public async Task<Result> UpdateAsync(DnsProviderSettings settings, string domain, IPAddress ipAddress, CancellationToken cancellationToken)
		{
			var dataResult = await RequestApi(settings, "api/dns/getData", $@"{{""fqdn"":""{domain}""}}", cancellationToken);

			var records = dataResult["records"]!;

			var ips = ((JsonArray)records["A"]!)
				.Select(x => IPAddress.Parse(x!["address"]!.GetValue<string>()))
				.ToList();

			if (ips.Contains(ipAddress))
			{
				var ipsString = string.Join(", ", ips.Select(x => x.ToString()));

				logger.LogInformation("Record A from API {ips} already contains current address {ipAddress}", ipsString, ipAddress);
			
				return new Result
				{
					Success = false,
					Error = $"Record A from API {ipsString} already contains current address {ipAddress} - DNS propagation in progress."
				};
			}

			var priority = settings.ConfigurationSection?.GetValue<int?>("priority") ?? 10;
		
			records["A"] = new JsonArray(new JsonObject
			{
				["priority"] = priority,
				["value"] = ipAddress.ToString()
			});

			var data = $@"{{""fqdn"":""{domain}"",""records"":{records}}}";

			var result = await RequestApi(settings, "api/dns/changeRecords", data, cancellationToken);

			return new Result
			{
				Success = result.GetValue<bool>()
			};
		}
	
		private async Task<JsonNode> RequestApi(DnsProviderSettings settings, string method, string data, CancellationToken cancellationToken)
		{
			var client = httpClientFactory.CreateClient();
			
			var uriBuilder = new UriBuilder(Uri.UriSchemeHttps, "api.beget.com")
			{
				Path = method,
				Query = new QueryString()
					.Add("input_format", "json")
					.Add("output_format", "json")
					.Add("login", settings.Username!)
					.Add("passwd", settings.Password!)
					.Add("input_data", data)
					.ToString()
			};
		
			var response = await client.GetAsync(uriBuilder.Uri, cancellationToken);

			response.EnsureSuccessStatusCode();

			var content = await response.Content.ReadAsStringAsync(cancellationToken);

			// example result of api/dns/changeRecords
			// {"status":"success","answer":{"status":"success","result":true}}
		
			// example result of api/dns/getData
			// {"status":"success","answer":{"status":"success",
			//      "result":{"is_under_control":false,"is_beget_dns":false,"is_subdomain":true,"fqdn":"sub.domain.net",
			//          "records":{
			//              "A":[{"ttl":600,"address":"xxx.xxx.xxx.xxx"}],
			//              "MX":[{"ttl":300,"exchange":"mx1.beget.com.","preference":10},{"ttl":300,"exchange":"mx2.beget.com.","preference":20}],
			//              "TXT":[{"ttl":300,"txtdata":"v=spf1 include:beget.com ~all"}],"DNS":[],"DNS_IP":[]},"set_type":1}}}

			var node = JsonNode.Parse(content)!;

			var responseStatus = node["status"]!.GetValue<string>();
			var answerStatus = node["answer"]!["status"]!.GetValue<string>();

			if (responseStatus == "success" && answerStatus == "success")
			{
				var result = node["answer"]!["result"]!;

				return result;
			}

			// todo: return Result instead of exception
			throw new InvalidOperationException(
				$"Failed to request API method {method}, response status: {responseStatus}, answer status: {answerStatus}\n"
				+ content);
		}
	}
}