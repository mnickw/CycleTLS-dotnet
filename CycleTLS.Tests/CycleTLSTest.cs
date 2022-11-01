using Microsoft.Extensions.Logging;

namespace CycleTLS.Tests
{
    public class CycleTLSTest
    {
        [Fact]
        public async Task Test1()
        {
            var logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger<CycleTLSClient>();
            CycleTLSClient client = new CycleTLSClient(logger);
            client.InitializeServerAndClient();
            client.DefaultRequestOptions.Ja3 = "";
            client.DefaultRequestOptions.UserAgent = "";

            try
			{
                CycleTLSResponse response = await client.SendAsync(HttpMethod.Get, "");
                Console.WriteLine(response);
            }
			catch (Exception e)
			{
				Console.WriteLine("Request Failed: " + e.Message);
				throw;
			}
        }
    }
}