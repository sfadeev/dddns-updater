using System.Net;
using DnsUpdater.Models;
using DnsUpdater.Services.Jobs;

namespace DnsUpdater.Services.DnsProviders
{
	public class TimewebDnsProvider(ILogger<TimewebDnsProvider> logger, TimewebHttpClient client) : IDnsProvider
	{
		public async Task<Result> UpdateAsync(DnsProviderSettings settings, string domain, IPAddress ipAddress, CancellationToken cancellationToken)
		{
			var userRecords = await client.GetUserRecords(settings, domain, cancellationToken);
			
			if (userRecords.Success == false) return userRecords.AsResult();

			var recordA = userRecords.Data?.Records?.FirstOrDefault(x => string.Equals("A", x.Type, StringComparison.OrdinalIgnoreCase));

			if (recordA != null)
			{
				var recordAddress = IPAddress.Parse(recordA.Value!);

				if (Equals(ipAddress, recordAddress))
				{
					logger.LogInformation("Record A from API {recordValue} already contains current address {ipAddress}", recordA.Value, ipAddress);
			
					return new Result
					{
						Success = false,
						Error = $"Record A from API {recordA.Value} already contains current address {ipAddress} - DNS propagation in progress."
					};
				}
				
				logger.LogDebug("Removing A record {recordValue} with ID {recordId} from DNS", recordA.Value, recordA.Id);

				var deleteResult = await  client.DeleteUserRecord(settings, domain, recordA.Id!.Value, cancellationToken);
				
				if (deleteResult.Success == false) return deleteResult.AsResult();
			}
			
			logger.LogDebug("Adding A record {ipAddress} to DNS", ipAddress);
			
			var addRecordA = new AddUserRecordRequest
			{
				Type = "A", Data = new UserRecord { Value = ipAddress.ToString() } 
			};
			
			var addResult = await client.AddUserRecord(settings, domain, addRecordA, cancellationToken);
			
			return addResult.AsResult();
		}
	}
}