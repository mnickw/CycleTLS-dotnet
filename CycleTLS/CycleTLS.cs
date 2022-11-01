using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using WebSocketSharp;
using static System.Net.WebRequestMethods;

namespace CycleTLS
{
    // TODO: logs
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
        /// Creates and runs server with source CycleTLS library and WebSocket client
        /// </summary>
        /// <param name="port"></param>
        /// <param name="debug"></param>
        /// <exception cref="InvalidOperationException">Server already initialized</exception>
        public void InitializeServerAndClient(int port = 9119, bool debug = false)
        {
            if (GoServer != null || DoesServerAlreadyRun(port))
            {
                throw new InvalidOperationException("Server already initialized");
            }

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

            StartServer(debug, executableFilename, port);

            StartClient(port, debug);
        }

        private bool DoesServerAlreadyRun(int port)
        {
            IPAddress ipAddress = Dns.GetHostEntry("localhost").AddressList[0];
            try
            {
                TcpListener tcpListener = new TcpListener(ipAddress, port);
                tcpListener.Start();
                tcpListener.Stop();
            }
            catch
            {
                return true;
            }
            return false;
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

        private void StartServer(bool debug, string filename, int port)
        {
            // TODO: solve problem with directories
            var pi = new ProcessStartInfo(filename);
            pi.EnvironmentVariables.Add("WS_PORT", port.ToString());
            pi.UseShellExecute = true;
            pi.WindowStyle = ProcessWindowStyle.Hidden;

            GoServer = new Process();
            GoServer.StartInfo = pi;
            GoServer.ErrorDataReceived += (_, ea) =>
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
                    GoServer.Kill();
                    // TODO: Dispose
                    StartServer(debug, filename, port);
                }
            };
            GoServer.Start();
        }

        private void StartClient(int port, bool debug)
        {
            var ws = new WebSocket("ws://localhost:" + port);

            ws.OnError += (_, ea) =>
            {
                ws.Close();
                Task.Delay(100).ContinueWith((t) => StartClient(port, debug));
            };

            ws.Connect();

            WebSocketClient = ws;
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

            if (GoServer == null)
            {
                throw new InvalidOperationException("Server with source CycleTLS library is not initialized");
            }

            if (WebSocketClient == null)
            {
                throw new InvalidOperationException("WebSocket client is not initialized");
            }

            // options + DefaultOptions
            var jsonRequestData = JsonSerializer.Serialize(new CycleTLSRequest()
            {
                RequestId = "", // generate requestId
                Options = cycleTLSRequestOptions
            });

    //        lastRequestID = requestId

    //if (this.server)
    //        {
    //            this.server.send(JSON.stringify({ requestId, options }));
    //        }
    //        else
    //        {
    //            if (this.queue == null)
    //            {
    //                this.queue = [];
    //            }
    //            this.queue.push(JSON.stringify({ requestId, options }))

    //  if (this.queueId == null)
    //            {
    //                this.queueId = setInterval(() => {
    //                    if (this.server)
    //                    {
    //                        for (let request of this.queue)
    //                        {
    //                            this.server.send(request);
    //                        }
    //                        this.queue = [];
    //                        clearInterval(this.queueId);
    //                        this.queueId = null
    //                    }
    //                }, 100)
    //  }
    //        }

            //instance.once(requestId, (response) => {
            //if (response.error) return rejectRequest(response.error);
            //try
            //{
            //    //parse json responses
            //    response.Body = JSON.parse(response.Body);
            //    //override console.log full repl to display full body
            //    response.Body[util.inspect.custom] = function(){ return JSON.stringify(this, undefined, 2); }
            //}
            //catch (e) { }

            //const { Status: status, Body: body, Headers: headers } = response;

            //if (headers["Set-Cookie"])
            //    headers["Set-Cookie"] = headers["Set-Cookie"].split("/,/");
            //resolveRequest({
            //    status,
            //    body,
            //    headers,
            //  });
            //});

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