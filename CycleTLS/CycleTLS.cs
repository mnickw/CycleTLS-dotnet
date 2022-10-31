using Microsoft.Extensions.Logging;
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
        private readonly ILogger<CycleTLSServer> _logger;

        public CycleTLSServer()
        {
            var factory = LoggerFactory.Create(b => b.AddConsole());
            var logger = factory.CreateLogger<CycleTLSServer>();
        }

        public CycleTLSServer(ILogger<CycleTLSServer> logger) =>
            _logger = logger;

        /// <summary>
        /// Creates and runs CycleTLS server
        /// </summary>
        public CycleTLSClient Initialize()
        {
            string executableFilename = "";
            try
            {
                executableFilename = GetExecutableFilename();
            }
            catch (PlatformNotSupportedException)
            {
                CleanExit("Operating system not supported");
                throw;
            }

            // Set CleanExit on SIGINT
            var sigintReceived = false;
            Console.CancelKeyPress += (_, ea) =>
            {
                // Tell .NET to not terminate the process
                ea.Cancel = true;

                CleanExit();
                sigintReceived = true;
            };
            // Set CleanExit on SIGTERM
            AppDomain.CurrentDomain.ProcessExit += (_, ea) =>
            {
                if (!sigintReceived)
                {
                    CleanExit();
                }
            };

            //handleSpawn(debug, executableFilename, port);

            //this.createClient(port, debug);

            throw new NotImplementedException();
        }

        public void CleanExit(string message = "", bool exit = true)
        {
            if (string.IsNullOrEmpty(message))
                _logger.LogError(message);

            //if (process.platform == "win32") {
            //  if(child) {
            //    new Promise((resolve, reject) => {
            //      exec(
            //          "taskkill /pid " + child.pid + " /T /F",
            //          (error: any, stdout: any, stderr: any) => {
            //            if (error) {
            //              console.warn(error);
            //            }
            //            if (exit) process.exit();
            //          }
            //      );
            //    });
            //  }
            //} else {
            //  if(child) {
            //    //linux/darwin os
            //    new Promise((resolve, reject) => {
            //      process.kill(-child.pid);
            //      if (exit) process.exit();
            //    });
            //  }
        }

        private static string GetExecutableFilename()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return "index.exe";
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                if (RuntimeInformation.OSArchitecture == Architecture.Arm)
                {
                    return "index-arm";
                }
                if (RuntimeInformation.OSArchitecture == Architecture.Arm64)
                {
                    return "index-arm64";
                }
                return "index";

            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return "index-mac";
            }
            throw new PlatformNotSupportedException();
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