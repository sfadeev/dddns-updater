using System.Xml.Serialization;
using DnsUpdater.Services.DnsProviders;
using DnsUpdater.Services.Jobs;
using Microsoft.Extensions.Logging.Abstractions;

namespace DnsUpdater.Tests.Services.DnsProviders
{
	public class NicRuHttpClientTest
	{
		[Test]
		public async Task ServicesApi_ForRealAccount_ShouldReturnAtLeastOne()
		{
			// arrange
			var cancellationToken = new CancellationTokenSource().Token;
			var client = new NicRuHttpClient(NullLogger<NicRuDnsProvider>.Instance, new HttpClient());
			var settings = GetSettings();
			
			// act
			var result = await client.Services(settings, cancellationToken);

			// assert
			Assert.That(result, Is.Not.Null);
			Assert.That(result.Success, Is.True);
			Assert.That(result.Data?.Data?.Services?.Length, Is.AtLeast(1));
		}
		
		[Test]
		public async Task ZonesApi_ForRealAccount_ShouldReturnAtLeastOne()
		{
			// arrange
			var cancellationToken = new CancellationTokenSource().Token;
			var client = new NicRuHttpClient(NullLogger<NicRuDnsProvider>.Instance, new HttpClient());
			var settings = GetSettings();
			
			// act
			var result = await client.Zones(settings, cancellationToken);

			// assert
			Assert.That(result, Is.Not.Null);
			Assert.That(result.Success, Is.True);
			Assert.That(result.Data?.Data?.Zones?.Length, Is.AtLeast(1));
		}

		[Test]
		public void ServiceResponse_WithDocumentationSample_ShouldDeserialize()
		{
			// arrange
			var serializer = new XmlSerializer(typeof(NicRuHttpClient.ServicesResponse));
			var content = @"<?xml version=""1.0"" encoding=""UTF-8"" ?>
<response>
<status>success</status>
<data>
<service admin=""123/NIC-REG"" domains-limit=""12"" domains-num=""5"" enable=""true"" has-primary=""false"" name=""testservice""
payer=""123/NIC-REG"" tariff=""Secondary L"" />
<service admin=""123/NIC-REG"" domains-limit=""150"" domains-num=""10"" enable=""true"" has-primary=""true"" name=""myservice""
payer=""123/NIC-REG"" rr-limit=""7500"" rr-num=""1000"" tariff=""DNS-master XXL"" />
</data>
</response>";
			
			// act
			var result = (NicRuHttpClient.ServicesResponse)serializer.Deserialize(new StringReader(content))!;
			
			// assert
			Assert.That(result, Is.Not.Null);
			Assert.That(result.Data?.Services?.Length, Is.EqualTo(2));
		}
		
		[Test]
		public void ZonesResponse_WithDocumentationSample_ShouldDeserialize()
		{
			// arrange
			var serializer = new XmlSerializer(typeof(NicRuHttpClient.ZonesResponse));
			var content = @"<?xml version=""1.0"" encoding=""UTF-8"" ?>
<response>
<status>success</status>
<data>
<zone admin=""123/NIC-REG"" enable=""true"" has-changes=""false"" has-primary=""true"" id=""227645"" idn-name=""тест.рф""
name=""xn—e1aybc.xn--p1ai"" payer=""123/NIC-REG"" service=""myservice"" />
<zone admin=""123/NIC-REG"" enable=""true"" has-changes=""false"" has-primary=""true"" id=""227642"" idn-name=""example.ru""
name=""example.ru"" payer=""123/NIC-REG"" service=""myservice"" />
<zone admin=""123/NIC-REG"" enable=""true"" has-changes=""false"" has-primary=""true"" id=""227643"" idn-name=""test.su""
name=""test.su"" payer=""123/NIC-REG"" service=""myservice"" />
</data>
</response>";
			
			// act
			var result = (NicRuHttpClient.ZonesResponse)serializer.Deserialize(new StringReader(content))!;
			
			// assert
			Assert.That(result, Is.Not.Null);
			Assert.That(result.Data?.Zones?.Length, Is.EqualTo(3));
		}

		private static DnsProviderSettings GetSettings()
		{
			return new DnsProviderSettings
			{
				Provider = "nicru",
				ClientId = Environment.GetEnvironmentVariable("NicRu.ClientId"),
				ClientSecret = Environment.GetEnvironmentVariable("NicRu.ClientSecret"),
				Username = Environment.GetEnvironmentVariable("NicRu.Username"),
				Password = Environment.GetEnvironmentVariable("NicRu.Password")
			};
		}
	}
}