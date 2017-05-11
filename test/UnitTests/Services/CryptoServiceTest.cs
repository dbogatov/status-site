using StatusMonitor.Shared.Services;
using Xunit;

namespace StatusMonitor.Tests.UnitTests.Services
{
	public class CryptoServiceTest
	{
		[Theory]
		[InlineData("password", "5f4dcc3b5aa765d61d8327deb882cf99")]
		[InlineData("adfs4565", "c57a19295696c283f4a645d600e3d4a4")]
		[InlineData("", "d41d8cd98f00b204e9800998ecf8427e")]
		[InlineData("aaaaaaaa", "3dbe00a167653a1aaee01d93e77e730e")]
		public void CalculatesHash(string plainText, string cipher)
		{
			// Arrange
			var cryptoService = new CryptoService();
			
			// Act
			var hash = cryptoService.CalculateHash(plainText);

			// Assert
			Assert.Equal(cipher.ToUpperInvariant(), hash);
		}
	}
}
