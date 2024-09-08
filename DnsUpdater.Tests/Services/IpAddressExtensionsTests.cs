using System.Net;
using DnsUpdater.Services;

namespace DnsUpdater.Tests.Services
{
	public class IpAddressExtensionsTests
	{
		[Test]
		[TestCase("10.10.10.10")]
		[TestCase("100.100.100.100")]
		[TestCase("172.10.1.1")]
		[TestCase("192.0.0.192")]
		[TestCase("192.168.1.1")]
		[TestCase("192.18.10.10")]
		[TestCase("192.19.10.10")]
		public void IsPrivate_ReturnsTrue_PrivateIpV4(string ip)
		{
			var ipAddress = IPAddress.Parse(ip);
		
			Assert.That(ipAddress.IsPrivateV4(), Is.True);
		}
		
		[Test]
		[TestCase("64.233.164.102")]
		[TestCase("193.168.47.254")]
		public void IsPrivate_ReturnsFalse_PublicIpV4(string ip)
		{
			var ipAddress = IPAddress.Parse(ip);
		
			Assert.That(ipAddress.IsPrivateV4(), Is.False);
		}
	}
}