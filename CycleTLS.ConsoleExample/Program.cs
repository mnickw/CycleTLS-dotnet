using CycleTLS;
using Microsoft.Extensions.Logging;

var logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger<CycleTLSClient>();

CycleTLSClient client = new CycleTLSClient(logger);

client.InitializeServerAndClient();

// To find your ja3 you can use https://kawayiyi.com/tls or https://tls.peet.ws/
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