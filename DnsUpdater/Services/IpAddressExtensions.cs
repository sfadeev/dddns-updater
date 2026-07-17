using System.Net;
using System.Net.Sockets;

namespace DnsUpdater.Services
{
	public static class IpAddressExtensions
	{
		/// <summary>
		/// Returns whether an address is unsuitable as a globally reachable IPv4
		/// address. The ranges follow the IANA IPv4 Special-Purpose Address
		/// Registry: https://www.iana.org/assignments/iana-ipv4-special-registry/
		/// </summary>
		public static bool IsNotGloballyReachableV4(this IPAddress ip)
		{
			var ipBytes = ip.GetAddressBytes();

			if (ip.AddressFamily != AddressFamily.InterNetwork)
			{
				return true;
			}

			return ipBytes[0] switch
			{
				// "This network", private-use, shared address space and loopback.
				0 or 10 or 127 => true,
				100 when ipBytes[1] is >= 64 and <= 127 => true,

				// Link-local and private-use ranges.
				169 when ipBytes[1] == 254 => true,
				172 when ipBytes[1] is >= 16 and <= 31 => true,
				192 when ipBytes[1] == 168 => true,

				// IETF assignments. Only 192.0.0.9 and 192.0.0.10 are globally reachable.
				192 when ipBytes[1] == 0 && ipBytes[2] == 0 && ipBytes[3] is not (9 or 10) => true,
				192 when ipBytes[1] == 0 && ipBytes[2] == 2 => true,
				192 when ipBytes[1] == 88 && ipBytes[2] == 99 => true,

				// Benchmarking and documentation ranges.
				198 when ipBytes[1] is 18 or 19 => true,
				198 when ipBytes[1] == 51 && ipBytes[2] == 100 => true,
				203 when ipBytes[1] == 0 && ipBytes[2] == 113 => true,

				// Reserved and limited broadcast.
				>= 240 => true,
				_ => false
			};
		}
	}
}
