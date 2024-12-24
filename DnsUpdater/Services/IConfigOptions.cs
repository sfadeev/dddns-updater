namespace DnsUpdater.Services
{
	public interface IConfigOptions
	{
		static abstract string SectionName { get; }
	}

	public static class ConfigOptionsExtensions
	{
		public static IHostApplicationBuilder Configure<TOptions>(
			this IHostApplicationBuilder builder) where TOptions : class, IConfigOptions
		{
			var section = builder.Configuration.GetSection(TOptions.SectionName);
			
			builder.Services.Configure<TOptions>(section);
			
			return builder;
		}

		public static TOptions? GetConfigurationSection<TOptions>(
			this IHostApplicationBuilder builder) where TOptions : class, IConfigOptions
		{
			return builder.Configuration
				.GetSection(TOptions.SectionName)
				.Get<TOptions>();
		}
	}
}