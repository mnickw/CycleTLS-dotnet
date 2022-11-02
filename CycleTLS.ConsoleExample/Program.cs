using CycleTLS;
using Microsoft.Extensions.Logging;

var logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger<CycleTLSClient>();

CycleTLSClient client = new CycleTLSClient(logger);

client.InitializeServerAndClient();

// To find your ja3 you can use https://kawayiyi.com/tls or https://tls.peet.ws/
client.DefaultRequestOptions.Ja3 = "771,4865-4866-4867-49195-49199-49196-49200-52393-52392-49171-49172-156-157-47-53,0-23-65281-10-11-35-16-5-13-18-51-45-43-27-17513-21,29-23-24,0";
client.DefaultRequestOptions.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
    "AppleWebKit/537.36 (KHTML, like Gecko) Chrome/105.0.0.0 Safari/537.36";

try
{
    CycleTLSResponse response = await client.SendAsync(HttpMethod.Get, "https://lift-api.vfsglobal.com/master/centerwithslots/pol/tur/NVWP2/en-US");
    Console.WriteLine(response);
}
catch (Exception e)
{
    Console.WriteLine("Request Failed: " + e.Message);
    throw;
}