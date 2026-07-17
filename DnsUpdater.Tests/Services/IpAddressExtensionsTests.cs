using System.Net;
using DnsUpdater.Services;

namespace DnsUpdater.Tests.Services
{
	public class IpAddressExtensionsTests
	{
		[Test]
		[TestCase("0.0.0.0")]
		[TestCase("10.10.10.10")]
		[TestCase("100.64.0.0")]
		[TestCase("100.127.255.255")]
		[TestCase("127.0.0.1")]
		[TestCase("169.254.1.1")]
		[TestCase("172.16.0.0")]
		[TestCase("172.31.255.255")]
		[TestCase("192.0.0.8")]
		[TestCase("192.0.0.11")]
		[TestCase("192.0.2.1")]
		[TestCase("192.88.99.1")]
		[TestCase("192.168.1.1")]
		[TestCase("198.18.0.1")]
		[TestCase("198.19.255.255")]
		[TestCase("198.51.100.1")]
		[TestCase("203.0.113.1")]
		[TestCase("240.0.0.1")]
		[TestCase("255.255.255.255")]
		[TestCase("::1")]
		public void IsNotGloballyReachableV4_ReturnsTrue_ForSpecialAddress(string ip)
		{
			var ipAddress = IPAddress.Parse(ip);
		
			Assert.That(ipAddress.IsNotGloballyReachableV4(), Is.True);
		}
		
		[Test]
		[TestCase("8.8.8.8")]
		[TestCase("100.63.255.255")]
		[TestCase("100.128.0.0")]
		[TestCase("172.15.255.255")]
		[TestCase("172.32.0.0")]
		[TestCase("192.0.0.9")]
		[TestCase("192.0.0.10")]
		[TestCase("192.18.10.10")]
		[TestCase("198.17.255.255")]
		[TestCase("198.20.0.0")]
		[TestCase("64.233.164.102")]
		[TestCase("193.168.47.254")]
		public void IsNotGloballyReachableV4_ReturnsFalse_ForGloballyReachableAddress(string ip)
		{
			var ipAddress = IPAddress.Parse(ip);
		
			Assert.That(ipAddress.IsNotGloballyReachableV4(), Is.False);
		}
	}
}
