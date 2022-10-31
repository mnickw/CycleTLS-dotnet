namespace CycleTLS.Tests
{
    public class CycleTLSTest
    {
        [Fact]
        public async Task Test1()
        {
			CycleTLSClient client = CycleTLSServer.Initialize();
            client.DefaultRequestOptions.Ja3 = "";
            client.DefaultRequestOptions.UserAgent = "";

            CycleTLSResponse response = null;
            try
			{
				response = await client.SendAsync(HttpMethod.Get, "");
			}
			catch (Exception e)
			{
				Console.WriteLine("Request Failed: " + e.Message);
				throw;
			}
			Console.WriteLine(response);
        }
    }
}