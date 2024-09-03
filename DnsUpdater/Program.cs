using DnsUpdater.Services;
using DnsUpdater.Services.DnsProviders;
using DnsUpdater.Services.IpProviders;

namespace DnsUpdater
{
	public abstract class Program
	{
		public static void Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);
			
			builder.Configuration.AddJsonFile("./data/settings.json", true, true);
			
			builder.Services.AddControllersWithViews();

			builder.Services
				.AddHttpClient()
				.AddHostedService<HostedService>()
				
				.AddServiceKeyProvider()
			
				.AddSingleton<IIpProvider, DefaultIpProvider>()
				.AddKeyedTransient<IIpProvider, IfconfigIpProvider>("ifconfig")
				.AddKeyedTransient<IIpProvider, IpifiIpProvider>("ipify")
			
				.AddKeyedTransient<IDnsProvider, BegetDnsProvider>("beget");

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
	}
}