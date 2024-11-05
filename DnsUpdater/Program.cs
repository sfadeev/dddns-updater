using DnsUpdater.Services;
using DnsUpdater.Services.DnsProviders;
using DnsUpdater.Services.IpProviders;
using Quartz;
using Serilog;
using Serilog.Extensions.Logging;

namespace DnsUpdater
{
	public abstract class Program
	{
		public const string ConfigFilePath = "./data/settings.json";
		public const string UpdatesFilePath = "./data/updates.json";
		public const string BackupDirPath = "./data/";
		public const string BackupFilePrefix = "backup";
		
		public static void Main(string[] args)
		{
			Log.Logger = new LoggerConfiguration()
				.Enrich.FromLogContext()
				.WriteTo.Console()
				.CreateBootstrapLogger();

			var logger = new SerilogLoggerFactory(Log.Logger).CreateLogger<Program>();

			try
			{
				var builder = WebApplication.CreateBuilder(args);

				builder.Configuration.AddJsonFile(ConfigFilePath, true, true);

				builder.Services
					.AddSerilog((services, lc) => lc
						.ReadFrom.Configuration(builder.Configuration)
						.ReadFrom.Services(services));

				builder.Services
					.AddHttpClient()
					.AddServiceKeyProvider()
					.AddHostedService<BackgroundMessenger>()
					.AddSingleton<IIpProvider, DefaultIpProvider>()
					.AddSingleton<IUpdateStorage, JsonUpdateStorage>()
					.AddSingleton<IMessageSender, AppriseMessageSender>()
					.AddSingleton<IHealthcheckService, HealthcheckIoService>()

					.AddKeyedTransient<IIpProvider, IfconfigIpProvider>("ifconfig")
					.AddKeyedTransient<IIpProvider, IpifiIpProvider>("ipify")
					.AddKeyedTransient<IIpProvider, IdentIpProvider>("ident")
					.AddKeyedTransient<IIpProvider, NnevIpProvider>("nnev")
					.AddKeyedTransient<IIpProvider, WtfismyipIpProvider>("wtfismyip")
					.AddKeyedTransient<IIpProvider, SeeipIpProvider>("seeip")

					.AddKeyedTransient<IDnsProvider, BegetDnsProvider>("beget")
					.AddKeyedTransient<IDnsProvider, TimewebDnsProvider>("timeweb")
					
					.AddTransient<TimewebHttpClient>()
					
					.AddQuartz(quartz =>
					{
						quartz
							.AddJob<UpdateDnsJob>(logger, builder.Configuration)
							.AddJob<BackupConfigJob>(logger, builder.Configuration);
					})
					.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
				
				builder.Services.AddControllersWithViews();

				var app = builder.Build();

				if (app.Environment.IsDevelopment() == false)
				{
					app.UseExceptionHandler("/Home/Error");
					// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
					app.UseHsts();
				}

				app.UseHttpsRedirection();
				app.UseStaticFiles();

				app.UseRouting();

				app.UseAuthorization();

				app.MapControllerRoute(
					"default",
					"{controller=Home}/{action=Index}/{id?}");

				app.Run();
			}
			catch (Exception ex)
			{
				Log.Fatal(ex, "Application terminated unexpectedly");
			}
			finally
			{
				Log.CloseAndFlush();
			}
		}
	}
}