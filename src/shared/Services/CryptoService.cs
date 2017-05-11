using System.Security.Cryptography;
using System.Text;

namespace StatusMonitor.Shared.Services
{
	public interface ICryptoService
	{
		/// <summary>
		/// Generates MD5 hash of given input
		/// </summary>
		/// <param name="input">Input for which to compute hash</param>
		/// <returns>MD5 hash</returns>
		string CalculateHash(string input);
	}

	public class CryptoService : ICryptoService
	{
		public string CalculateHash(string input)
		{
			// step 1, calculate MD5 hash from input
			MD5 md5 = System.Security.Cryptography.MD5.Create();
			byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
			byte[] hash = md5.ComputeHash(inputBytes);

			// step 2, convert byte array to hex string
			StringBuilder sb = new StringBuilder();

			for (int i = 0; i < hash.Length; i++)
			{
				sb.Append(hash[i].ToString("X2"));
			}

			return sb.ToString();
		}
	}
}
