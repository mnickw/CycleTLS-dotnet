using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using WebSocketSharp;
using static System.Net.WebRequestMethods;

namespace CycleTLS
{
    // TODO: ConfigureAwait
    // TODO: Ask why websockets, not just usual http
    public class CycleTLSClient
    {
        private readonly ILogger<CycleTLSClient> _logger;

        // TODO: Dispose
        private WebSocket WebSocketClient { get; set; } = null;
        private Process GoServer { get; set; } = null;

        public CycleTLSRequestOptions DefaultRequestOptions { get; } = new CycleTLSRequestOptions()
        {
            Ja3 = "771,4865-4867-4866-49195-49199-52393-52392-49196-49200-49162-49161-49171-49172-51-57-47-53-10,0-23-65281-10-11-35-16-5-51-43-13-45-28-21,29-23-24-25-256-257,0",
            UserAgent = "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/101.0.4951.54 Safari/537.36"
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

            CreateClient(port, debug);
        }

        private bool IsPortAvailable(int port)
        {
            IPAddress ipAddress = Dns.GetHostEntry("localhost").AddressList[0];
            try
            {
                TcpListener tcpListener = new TcpListener(ipAddress, port);
                tcpListener.Start();
            }
            catch
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

            CreateClient(port, debug);
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

        private void CreateClient(int port, bool debug)
        {
            var ws = new WebSocket("ws://localhost:" + port);

            ws.OnOpen += (_, ea) =>
            {
                WebSocketClient = ws;
            };

            ws.OnMessage += (_, ea) =>
            {
                var message = ea.Data;
                //parse json
                //add response to parallelDictionary requestId -> response
            };

            ws.OnError += (_, ea) =>
            {
                // TODO: check if no deadlock here
                // TODO: Dispose
                ws.Close();
                Task.Delay(100).ContinueWith((t) => CreateClient(port, debug));
            };

            ws.Connect();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="httpMethod"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<CycleTLSResponse> SendAsync(HttpMethod httpMethod, string url)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cycleTLSRequestOptions"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<CycleTLSResponse> SendAsync(CycleTLSRequestOptions cycleTLSRequestOptions)
        {
            // TODO: Simple cookies

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
        public string URL { get; set; } = ""; //`json:"url"`
	    public string Method { get; set; } = ""; //`json:"method"`
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>(); //`json:"headers"`
	    public string Body { get; set; } = ""; //`json:"body"`
        public string Ja3 { get; set; } = ""; //`json:"ja3"`
        public string UserAgent { get; set; } = "";  //`json:"userAgent"`
	    public string Proxy { get; set; } = "";  //`json:"proxy"`
        public List<Cookie> Cookies { get; set; } = new List<Cookie>(); //`json:"cookies"`
        public string Timeout { get; set; } = ""; //`json:"timeout"`
        public string DisableRedirect { get; set; } = ""; //`json:"disableRedirect"`
        public List<string> HeaderOrder { get; set; } = new List<string>(); //`json:"headerOrder"`
	    public string OrderAsProvided { get; set; } = ""; //`json:"orderAsProvided"` //TODO
    }
}