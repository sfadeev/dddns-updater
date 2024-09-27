using DnsUpdater.Services;
using DnsUpdater.Services.DnsProviders;
using DnsUpdater.Services.IpProviders;
using Serilog;

namespace DnsUpdater
{
	public abstract class Program
	{
		public static void Main(string[] args)
		{
			Log.Logger = new LoggerConfiguration()
				.Enrich.FromLogContext()
				.WriteTo.Console()
				.CreateBootstrapLogger();

			try
			{
				var builder = WebApplication.CreateBuilder(args);

				builder.Configuration.AddJsonFile("./data/settings.json", true, true);

				builder.Services
					.AddSerilog((services, lc) => lc
						.ReadFrom.Configuration(builder.Configuration)
						.ReadFrom.Services(services));
					
				builder.Services
					.AddHttpClient()
					.AddServiceKeyProvider()
					.AddHostedService<HostedService>()
					.AddSingleton<IIpProvider, DefaultIpProvider>()
					.AddSingleton<IUpdateStorage, JsonUpdateStorage>()
					.AddSingleton<IMessageSender, AppriseMessageSender>()
					.AddSingleton<IHealthcheckService, HealthcheckIoService>()
					
					.AddKeyedTransient<IIpProvider, IfconfigIpProvider>("ifconfig")
					.AddKeyedTransient<IIpProvider, IpifiIpProvider>("ipify")
					
					.AddKeyedTransient<IDnsProvider, BegetDnsProvider>("beget");

				// todo: add docker HEALTHCHECK
				
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