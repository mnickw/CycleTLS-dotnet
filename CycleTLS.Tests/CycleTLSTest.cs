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

            // To find your ja3 you can use https://kawayiyi.com/tls or https://tls.peet.ws/
            // Lib uses default Ja3 = "771,4865-4867-4866-49195-49199-52393-52392-49196-49200-49162-49161-49171-49172-51-57-47-53-10,0-23-65281-10-11-35-16-5-51-43-13-45-28-21,29-23-24-25-256-257,0"
            // Lib uses default UserAgent = "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/101.0.4951.54 Safari/537.36"
            //client.DefaultRequestOptions.Ja3 = "";
            //client.DefaultRequestOptions.UserAgent = "";

            try
            {
                CycleTLSRequestOptions options = new CycleTLSRequestOptions()
                {
                    Url = "https://kawayiyi.com/tls",
                    Method = HttpMethod.Get.Method,
                    //Body = ""
                    //Proxy = "http://USERNAME:PASSWORD@IP:PORT",
                    //Headers = new Dictionary<string, string>() { { "authorization", authToken } }
                };
                CycleTLSResponse response = await client.SendAsync(options);
                Console.WriteLine(response.Body);

                // Or just:
                Console.WriteLine((await client.SendAsync(HttpMethod.Get, "https://kawayiyi.com/tls")).Body);
            }
            catch (Exception e)
            {
                Console.WriteLine("Request Failed: " + e.Message);
                throw;
            }
        }
    }
}