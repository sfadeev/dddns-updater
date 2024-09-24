using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DnsUpdater.Services
{
	public interface IUpdateStorage
	{
		static readonly int MaxUpdates = 10;
		
		Task<DbUpdates> Store(string domain, IPAddress ip, string provider, bool success, string? message, CancellationToken cancellationToken);
		
		Task<DbUpdates> Query(CancellationToken cancellationToken);

		// Task Backup(CancellationToken cancellationToken);
	}
	
	public class DbUpdates
	{
		public DateTime LastDate { get; set; }

		public IList<DbDomain> Records { get; set; } = new List<DbDomain>();
	}
	
	public class DbDomain
	{
		public required string Domain { get; set; }
		
		public IList<DbDomainUpdate> Updates { get; set; } = new List<DbDomainUpdate>();
	}
	
	public class DbDomainUpdate
	{
		public DateTime Date { get; set; }
		
		public string? Provider { get; set; }
		
		public string? Ip { get; set; }
		
		public bool Success { get; set; }
		
		public string? Message { get; set; }
	}
	
	public class JsonUpdateStorage(ILogger<JsonUpdateStorage> logger) : IUpdateStorage
	{
		private const string UpdatesFilePath = "./data/updates.json";

		private readonly JsonSerializerOptions _jsonOptions = new()
		{
			WriteIndented = true, 
			PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower, 
			DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
		};

		// todo: create AsyncLock and use with using (var lock = await _lock.LockAsync()) { ... }
		private readonly SemaphoreSlim _semaphore = new(1, 1);
		
		private async Task<DbUpdates> ReadUpdates(CancellationToken cancellationToken)
		{
			DbUpdates? result = null;
					
			if (File.Exists(UpdatesFilePath))
			{
				var content = await File.ReadAllTextAsync(UpdatesFilePath, cancellationToken);
					
				result = JsonSerializer.Deserialize<DbUpdates>(content, _jsonOptions);
			}
				
			return result ?? new DbUpdates();
		}
		
		public async Task<DbUpdates> Store(string domain, IPAddress ip, string provider, bool success, string? message, CancellationToken cancellationToken)
		{
			logger.LogDebug("Storing {ip} for {domain} to {provider}", ip, domain, provider);

			await _semaphore.WaitAsync(cancellationToken);

			try
			{
				var now = DateTime.Now;

				var dbUpdates = await ReadUpdates(cancellationToken);

				var dbDomain = dbUpdates.Records.FirstOrDefault(x => x.Domain == domain);

				if (dbDomain == null)
				{
					dbDomain = new DbDomain { Domain = domain };

					dbUpdates.Records.Add(dbDomain);
				}

				dbDomain.Updates.Add(new DbDomainUpdate
				{
					Date = now,
					Provider = provider,
					Ip = ip.ToString(),
					Success = success,
					Message = message
				});

				if (dbDomain.Updates.Count > IUpdateStorage.MaxUpdates)
				{
					dbDomain.Updates.RemoveAt(0);
				}

				dbUpdates.LastDate = now;

				var json = JsonSerializer.Serialize(dbUpdates, _jsonOptions);

				await File.WriteAllTextAsync(UpdatesFilePath, json, cancellationToken);

				return dbUpdates;
			}
			finally
			{
				_semaphore.Release();
			}
		}

		public async Task<DbUpdates> Query(CancellationToken cancellationToken)
		{
			logger.LogDebug("Querying records");
			
			await _semaphore.WaitAsync(cancellationToken);

			try
			{
				return await ReadUpdates(cancellationToken);
			}
			finally
			{
				_semaphore.Release();
			}
		}
		
		/*public async Task Backup(CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}*/
	}
}