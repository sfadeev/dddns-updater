using System.IO.Compression;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using DnsUpdater.Models;
using Microsoft.Extensions.Options;

namespace DnsUpdater.Services
{
	public interface IUpdateStorage
	{
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

	public class JsonUpdateStorage(ILogger<JsonUpdateStorage> logger, IOptions<AppOptions> appOptions) : IUpdateStorage
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
					
			var options = appOptions.Value;
			
			if (File.Exists(options.UpdatesFilePath))
			{
				var content = await File.ReadAllTextAsync(options.UpdatesFilePath, cancellationToken);
					
				result = JsonSerializer.Deserialize<DbUpdates>(content, _jsonOptions);
			}
				
			return result ?? new DbUpdates();
		}
		
		public async Task<DbUpdates> Store(string domain, IPAddress ip, string provider, bool success, string? message, CancellationToken cancellationToken)
		{
			logger.LogDebug("Storing {Ip} for {Domain} to {Provider}", ip, domain, provider);

			using (await _lock.LockAsync(cancellationToken))
			{
				var options = appOptions.Value;
				
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

				if (dbDomain.Updates.Count > options.MaxUpdatesPerDomain)
				{
					dbDomain.Updates.RemoveAt(0);
				}

				dbUpdates.LastDate = now;

				var json = JsonSerializer.Serialize(dbUpdates, _jsonOptions);

				await File.WriteAllTextAsync(options.UpdatesFilePath, json, cancellationToken);

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
		
		public async Task<Result<FileInfo>> Backup(BackupMode mode, CancellationToken cancellationToken)
		{
			var options = appOptions.Value;
		
			var configFiles = new[] { Program.ConfigFilePath, options.UpdatesFilePath };

			var shouldBackup = ShouldBackup(mode, configFiles);

			if (shouldBackup)
			{
				var backupFilePath = Path.Combine(options.BackupDirPath, 
					$"{options.BackupFileNamePrefix}-{DateTime.Now.ToString("s").Replace(":", "-")}.zip");

				using (var stream = new FileStream(backupFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
				{
					using (var archive = new ZipArchive(stream, ZipArchiveMode.Create))
					{
						foreach (var backupFile in configFiles.Where(File.Exists))
						{
							archive.CreateEntryFromFile(backupFile, new FileInfo(backupFile).Name);
						}
					}
				}

				var fileInfo = new FileInfo(backupFilePath);
				
				logger.LogInformation("Created backup {FileName}, size {FileLength} bytes.", fileInfo.Name, fileInfo.Length);

				await RemoveOldBackups();
				
				return Result.CreateSuccessResult(fileInfo);
			}
			
			return Result.CreateErrorResult<FileInfo>();
		}

		private Task<int> RemoveOldBackups()
		{
			var options = appOptions.Value;
			
			var oldBackups = EnumerateBackupFiles()
				.OrderByDescending(x => x.LastWriteTime)
				.Skip(options.MaxBackups);

			var result = 0;
			
			foreach (var fileInfo in oldBackups)
			{
				logger.LogDebug("Removing old backup {FilePath}", fileInfo.FullName);
				
				fileInfo.Delete();
				
				result++;
			}
			
			return Task.FromResult(result);
		}

		/// <summary>
		/// Check if config files are more recent than last backup.
		/// </summary>
		/// <param name="mode"></param>
		/// <param name="configFiles"></param>
		/// <returns></returns>
		private bool ShouldBackup(BackupMode mode, IEnumerable<string> configFiles)
		{
			if (mode == BackupMode.Force) return true;
			
			var lastWrite = configFiles
				.Select(x => new FileInfo(x))
				.Where(x => x.Exists)
				.Select(x => x.LastWriteTime)
				.LastOrDefault();
			
			var lastBackup = EnumerateBackupFiles()
				.Select(x => x.LastWriteTime)
				.OrderDescending()
				.FirstOrDefault();

			var result = lastBackup < lastWrite;

			if (result)
			{
				logger.LogDebug("Backup required - last config write: {LastWrite}, last backup: {LastBackup}.", lastWrite, lastBackup);	
			}
			
			return result;
		}

		private IEnumerable<FileInfo> EnumerateBackupFiles()
		{
			var options = appOptions.Value;
			
			return new DirectoryInfo(options.BackupDirPath)
				.EnumerateFiles(options.BackupFileNamePrefix + "*" + ".zip");
		}
	}
}