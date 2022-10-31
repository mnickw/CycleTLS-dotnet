using System.Net.Http;
using System.Threading.Tasks;

namespace CycleTLS
{
    public class CycleTLSServer
    {
        /// <summary>
        /// Creates and runs CycleTLS server
        /// </summary>
        public static CycleTLSClient Initialize()
        {
            return new CycleTLSClient();
        }
    }

    public class CycleTLSClient
    {
        public CycleTLSRequestOptions DefaultRequestOptions { get; } = new CycleTLSRequestOptions()
        {

        };

        public async Task<CycleTLSResponse> SendAsync(HttpMethod httpMethod, string url)
        {

        }

        public async Task<CycleTLSResponse> SendAsync(CycleTLSRequestOptions cycleTLSRequestOptions)
        {

        }
    }

    public class CycleTLSResponse
    {

    }

    public class CycleTLSRequest
    {

    }

    public class CycleTLSRequestOptions
    {

    }
}