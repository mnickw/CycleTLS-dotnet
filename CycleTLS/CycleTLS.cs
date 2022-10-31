using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace CycleTLS
{
    public class CycleTLSClient
    {
        private readonly ILogger<CycleTLSClient> _logger;

        private Process GoServer { get; set; } = null;

        public CycleTLSRequestOptions DefaultRequestOptions { get; } = new CycleTLSRequestOptions()
        {

        };

        public CycleTLSClient(ILogger<CycleTLSClient> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Creates and runs server with source CycleTLS library
        /// </summary>
        /// <param name="port"></param>
        /// <param name="debug"></param>
        public void InitializeServer(int port = 9119, bool debug = false)
        {
            if (IsPortAvailable(port))
            {
                SpawnServer(port, debug);
                return;
            }

            //CreateClient(port, debug);
        }

        private bool IsPortAvailable(int port)
        {
            IPAddress ipAddress = Dns.GetHostEntry("localhost").AddressList[0];
            try
            {
                TcpListener tcpListener = new TcpListener(ipAddress, port);
                tcpListener.Start();
            }
            catch (SocketException ex)
            {
                return false;
            }
            return true;
        }

        private void SpawnServer(int port, bool debug)
        {
            string executableFilename = "";
            try
            {
                executableFilename = GetExecutableFilename();
            }
            catch (PlatformNotSupportedException)
            {
                _logger.LogError("Operating system not supported");
                throw;
            }

            HandleSpawn(debug, executableFilename, port);

            //CreateClient(port, debug);
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

        private void HandleSpawn(bool debug, string filename, int port)
        {
            // TODO: solve problem with directories
            var pi = new ProcessStartInfo(filename);
            pi.EnvironmentVariables.Add("WS_PORT", port.ToString());
            pi.UseShellExecute = true;
            pi.WindowStyle = ProcessWindowStyle.Hidden;

            var child = new Process();
            child.StartInfo = pi;
            child.ErrorDataReceived += async (_, ea) =>
            {
                if (ea.Data.Contains("Request_Id_On_The_Left"))
                {
                    var splitRequestIdAndError = ea.Data.Split(new string[] { "Request_Id_On_The_Left" }, StringSplitOptions.None);
                    var requestId = splitRequestIdAndError[0];
                    var error = splitRequestIdAndError[1];
                    // TODO: check source js code here
                    //_logger.LogError($"Error from CycleTLSClient: requestId:{requestId} error:{error}");
                }
                else
                {
                    // TODO: check source js code here
                    _logger.LogError($"Error from CycleTLSClient (please open an issue https://github.com/Danny-Dasilva/CycleTLS/issues/new/choose " +
                        $"or https://github.com/mnickw/CycleTLS-dotnet/issues): {ea.Data}");
                    // TODO: check that this will work
                    child.Kill();
                    HandleSpawn(debug, filename, port);
                }
            };
        }

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