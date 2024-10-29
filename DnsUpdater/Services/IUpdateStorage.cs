using System.IO.Compression;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using DnsUpdater.Models;

namespace DnsUpdater.Services
{
	public interface IUpdateStorage
	{
		static readonly int MaxUpdates = 10;
		
		Task<DbUpdates> Store(string domain, IPAddress ip, string provider, bool success, string? message, CancellationToken cancellationToken);
		
		Task<DbUpdates> Query(CancellationToken cancellationToken);

		Task<Result<FileInfo>> Backup(BackupMode mode, CancellationToken cancellationToken);
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

	public enum BackupMode
	{
		Auto = 0,
		Force = 1
	}

	public class JsonUpdateStorage(ILogger<JsonUpdateStorage> logger) : IUpdateStorage
	{
		private readonly JsonSerializerOptions _jsonOptions = new()
		{
			WriteIndented = true, 
			PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower, 
			DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
		};
		
		private readonly AsyncLock _lock = new();
		
		private async Task<DbUpdates> ReadUpdates(CancellationToken cancellationToken)
		{
			DbUpdates? result = null;
					
			if (File.Exists(Program.UpdatesFilePath))
			{
				var content = await File.ReadAllTextAsync(Program.UpdatesFilePath, cancellationToken);
					
				result = JsonSerializer.Deserialize<DbUpdates>(content, _jsonOptions);
			}
				
			return result ?? new DbUpdates();
		}
		
		public async Task<DbUpdates> Store(string domain, IPAddress ip, string provider, bool success, string? message, CancellationToken cancellationToken)
		{
			logger.LogDebug("Storing {ip} for {domain} to {provider}", ip, domain, provider);

			using (await _lock.LockAsync(cancellationToken))
			{
				var now = DateTime.Now;

				var dbUpdates = await ReadUpdates(cancellationToken);

				var dbDomain = dbUpdates.Records.FirstOrDefault(x => string.Equals(x.Domain, domain, StringComparison.InvariantCultureIgnoreCase));

				if (dbDomain == null)
				{
					dbDomain = new DbDomain { Domain = domain.ToLowerInvariant() };

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

				await File.WriteAllTextAsync(Program.UpdatesFilePath, json, cancellationToken);

				return dbUpdates;
			}
		}

		public async Task<DbUpdates> Query(CancellationToken cancellationToken)
		{
			logger.LogDebug("Querying records");
			
			using (await _lock.LockAsync(cancellationToken))
			{
				return await ReadUpdates(cancellationToken);
			}
		}

		private readonly string[] _backupFiles = [ Program.ConfigFilePath, Program.UpdatesFilePath ];
		
		public Task<Result<FileInfo>> Backup(BackupMode mode, CancellationToken cancellationToken)
		{
			var shouldBackup = ShouldBackup(mode);

			if (shouldBackup)
			{
				var backupFilePath = Path.Combine(Program.BackupDirPath, 
					$"{Program.BackupFilePrefix}-{DateTime.Now.ToString("s").Replace(":", "-")}.zip");

				using (var stream = new FileStream(backupFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
				{
					using (var archive = new ZipArchive(stream, ZipArchiveMode.Create))
					{
						foreach (var backupFile in _backupFiles)
						{
							archive.CreateEntryFromFile(backupFile, new FileInfo(backupFile).Name);
						}
					}
				}

				var fileInfo = new FileInfo(backupFilePath);
				
				logger.LogInformation("Created backup {name}, size {size} bytes.", fileInfo.Name, fileInfo.Length);

				// todo: remove old backups
				return Task.FromResult(Result.CreateSuccessResult(fileInfo));
			}
			
			return Task.FromResult(Result.CreateErrorResult<FileInfo>());
		}

		private bool ShouldBackup(BackupMode mode)
		{
			if (mode == BackupMode.Force) return true;
			
			var lastWrite = _backupFiles
				.Select(x => new FileInfo(x).LastWriteTime).LastOrDefault();

			var lastBackup = new DirectoryInfo(Program.BackupDirPath)
				.EnumerateFiles(Program.BackupFilePrefix + "*" + ".zip")
				.Select(x => x.LastWriteTime)
				.OrderDescending()
				.FirstOrDefault();

			var result = lastBackup < lastWrite;

			if (result)
			{
				logger.LogDebug("Backup required - last config write: {lastWrite}, last backup: {lastBackup}.", lastWrite, lastBackup);	
			}
			
			return result;
		}
	}
}