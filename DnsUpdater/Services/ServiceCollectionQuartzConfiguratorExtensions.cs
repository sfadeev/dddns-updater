using Quartz;

namespace DnsUpdater.Services
{
	public static class ServiceCollectionQuartzConfiguratorExtensions
	{
		public static IServiceCollectionQuartzConfigurator AddJob<TJob>(
			this IServiceCollectionQuartzConfigurator quartz, ILogger logger, IConfiguration configuration) where TJob : IJob
		{
			var jobName = GetJobName<TJob>();

			var configKey = $"Quartz:{jobName}";
			
			var cronSchedule = configuration[configKey];
			
			if (cronSchedule != null)
			{
				var jobKey = new JobKey(jobName);

				quartz.AddJob<TJob>(job => job.WithIdentity(jobKey));

				quartz.AddTrigger(trigger =>
				{
					trigger
						.WithIdentity(jobName + "-trigger")
						.ForJob(jobKey)
						.StartNow()
						.WithCronSchedule(cronSchedule);
				});
			}
			else
			{
				logger.LogWarning("No cron schedule configured for {TJob} with key {configKey}, ignoring.", typeof(TJob), configKey);
			}
			
			return quartz;
		}

		private static string GetJobName<TJob>() where TJob : IJob
		{
			const string trimAtEnd = "Job";

			var result = typeof(TJob).Name;

			return result.EndsWith(trimAtEnd) ? result[..^trimAtEnd.Length] : result;
		}
	}
}