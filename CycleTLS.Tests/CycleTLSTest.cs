namespace CycleTLS.Tests
{
    public class CycleTLSTest
    {
        [Fact]
        public async Task Test1()
        {
			CycleTLSClient client = CycleTLSServer.Initialize();
			CycleTLSClient.DefaultRequestOptions.Ja3 = "";
            CycleTLSClient.DefaultRequestOptions.UserAgent = "";

            try
			{
				CycleTLSResponse response = await CycleTLSClient.SendAsync(HttpMethod.Get, "");
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