using System.Net;
using System.Net.NetworkInformation;

namespace DnsUpdater.Services
{
	public static class IpAddressExtensions
	{
		/// <summary>
		/// https://en.wikipedia.org/wiki/Reserved_IP_addresses
		/// </summary>
		public static bool IsPrivateV4(this IPAddress ip)
		{
			var ipBytes = ip.GetAddressBytes();
			
			if (ipBytes.Length == 4)
			{
				switch (ipBytes[0])
				{
					// 10.0.0.0–10.255.255.255
					case 10:
						
					// 100.64.0.0–100.127.255.255
					case 100 when ipBytes[1] >= 64 && ipBytes[1] <= 127:
						return true;
					
					// 172.16.0.0–172.31.255.255
					case 172 when ipBytes[1] <= 31:
						return true;
					
					// 192.0.0.0–192.0.0.255
					case 192 when ipBytes[1] == 0 && ipBytes[2] == 0:
						return true;
					
					// 192.168.0.0–192.168.255.255
					case 192 when ipBytes[1] == 168:
						return true;

					// 198.18.0.0–198.19.255.255
					case 192 when ipBytes[1] >= 18 && ipBytes[1] <= 19:
						return true;
				}
			}

			return false;
		}
	}
}