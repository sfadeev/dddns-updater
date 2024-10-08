using Quartz;

namespace DnsUpdater.Services
{
	public static class ServiceCollectionQuartzConfiguratorExtensions
	{
		public static void AddJob<T>(
			this IServiceCollectionQuartzConfigurator quartz, IConfiguration configuration) where T : IJob
		{
			var jobName = typeof(T).Name;

			var cronSchedule = configuration[$"Quartz:{jobName}"];
			
			if (cronSchedule != null)
			{
				var jobKey = new JobKey(jobName);

				quartz.AddJob<T>(job => job.WithIdentity(jobKey));

				quartz.AddTrigger(trigger =>
				{
					trigger
						.WithIdentity(jobName + "-trigger")
						.ForJob(jobKey)
						.StartNow()
						.WithCronSchedule(cronSchedule);
				});
			}
		}
	}
}