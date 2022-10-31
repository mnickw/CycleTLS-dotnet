using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
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
            string executableFilename;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                executableFilename = "index.exe";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                if (RuntimeInformation.OSArchitecture == Architecture.Arm)
                {
                    executableFilename = "index-arm";
                }
                else if (RuntimeInformation.OSArchitecture == Architecture.Arm64)
                {
                    executableFilename = "index-arm64";
                }
                else
                {
                    executableFilename = "index";
                }

            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                executableFilename = "index-mac";
            }
            else
            {
                CleanExit("Operating system not supported");
            }
            //handleSpawn(debug, executableFilename, port);

            //this.createClient(port, debug);

            throw new NotImplementedException();
        }

        public static void CleanExit(string error)
        {

        }
    }

    public class CycleTLSClient
    {
        public CycleTLSRequestOptions DefaultRequestOptions { get; } = new CycleTLSRequestOptions()
        {

        };

        public async Task<CycleTLSResponse> SendAsync(HttpMethod httpMethod, string url)
        {
            throw new NotImplementedException();
        }

        public async Task<CycleTLSResponse> SendAsync(CycleTLSRequestOptions cycleTLSRequestOptions)
        {
            throw new NotImplementedException();
        }
    }

    public class CycleTLSResponse
    {
        public string RequestID { get; set; }
        public int Status { get; set; }
        public string Body { get; set; }
        public Dictionary<string, string> Headers { get; set; }
    }

    public class CycleTLSRequest
    {
        public string RequestId { get; set; }
        public CycleTLSRequestOptions Options { get; set; }
    }

    public class CycleTLSRequestOptions
    {
        public string URL { get; set; } //`json:"url"`
	    public string Method { get; set; } //`json:"method"`
	    public Dictionary<string,string> Headers { get; set; } //`json:"headers"`
	    public string Body { get; set; } //`json:"body"`
	    public string Ja3 { get; set; } //`json:"ja3"`
	    public string UserAgent { get; set; } //`json:"userAgent"`
	    public string Proxy { get; set; } //`json:"proxy"`
	    public Cookie[] Cookies { get; set; } //`json:"cookies"`
	    public string Timeout { get; set; } //`json:"timeout"`
	    public string DisableRedirect { get; set; } //`json:"disableRedirect"`
	    public string[] HeaderOrder { get; set; } //`json:"headerOrder"`
	    public string OrderAsProvided { get; set; } //`json:"orderAsProvided"` //TODO
    }
}