using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using WebSocketSharp;

namespace CycleTLS
{
    // TODO: Code SendAsync
    // TODO: Code QueueSendAsync
    // TODO: Check json parsing
    // TODO: Solve StartServer problems
    // TODO: Dispose
    // TODO: logs
    // TODO: Debug StartServer, StartClient and everything else

    // TODO: Documentation
    // TODO: explain in comments why do we need queue and dictionary
    // TODO: Simple cookies and headers
    // TODO: Ask why websockets, not just usual http
    public class CycleTLSClient
    {
        private readonly ILogger<CycleTLSClient> _logger;

        public TimeSpan DefaultTimeOut { get; private set; }


        private WebSocket WebSocketClient { get; set; } = null;
        private Process GoServer { get; set; } = null;


        private object _lockQueue = new object();

        private bool isQueueSendRunning = false;

        private Queue<(CycleTLSRequestOptions RequestOptions, TaskCompletionSource<CycleTLSResponse> RequestTCS)> RequstQueue { get; set; }
            = new Queue<(CycleTLSRequestOptions RequestOptions, TaskCompletionSource<CycleTLSResponse> RequestTCS)>();

        private ConcurrentDictionary<string, TaskCompletionSource<CycleTLSResponse>> SentRequests { get; set; }
            = new ConcurrentDictionary<string, TaskCompletionSource<CycleTLSResponse>>();

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
            // TODO:StartServer: solve problem with directories
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
                    // TODO:StartServer: check source js code here
                    //_logger.LogError($"Error from CycleTLSClient: requestId:{requestId} error:{error}");
                }
                else
                {
                    // TODO:StartServer: check source js code here
                    _logger.LogError($"Go server received error data (please open an issue https://github.com/Danny-Dasilva/CycleTLS/issues/new/choose " +
                        $"or https://github.com/mnickw/CycleTLS-dotnet/issues): {ea.Data}");
                    // TODO:StartServer: check that this will work
                    GoServer.Kill();
                    // TODO:StartServer: Dispose?
                    StartServer(debug, filename, port);
                }
            };
            GoServer.Start();
        }

        private void StartClient(int port, bool debug)
        {
            var ws = new WebSocket("ws://localhost:" + port);

            ws.OnMessage += (_, ea) =>
            {
                CycleTLSResponse response = JsonSerializer.Deserialize<CycleTLSResponse>(ea.Data);
                if (SentRequests.TryRemove(response.RequestID, out var requestTCS))
                {
                    requestTCS.TrySetResult(response);
                }
            };

            ws.OnError += (_, ea) =>
            {
                ws.Close(); // TODO:StartClient: Debug here for no errors

                foreach (var requestPair in SentRequests)
                {
                    requestPair.Value.TrySetException(ea.Exception);
                }

                SentRequests.Clear();

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
            return await SendAsync(httpMethod, url, DefaultTimeOut);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="httpMethod"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<CycleTLSResponse> SendAsync(HttpMethod httpMethod, string url, TimeSpan timeout)
        {
            return await SendAsync(new CycleTLSRequestOptions()
            {
                URL = url,
                Method = httpMethod.Method
            }, timeout);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="httpMethod"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<CycleTLSResponse> SendAsync(CycleTLSRequestOptions cycleTLSRequestOptions)
        {
            return await SendAsync(cycleTLSRequestOptions, DefaultTimeOut);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cycleTLSRequestOptions"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public Task<CycleTLSResponse> SendAsync(CycleTLSRequestOptions cycleTLSRequestOptions, TimeSpan timeout)
        {
            if (WebSocketClient == null)
            {
                throw new InvalidOperationException("WebSocket client is not initialized");
            }

            // TODO:SendAsync: options + DefaultOptions
            var jsonRequestData = JsonSerializer.Serialize(new CycleTLSRequest()
            {
                RequestId = "", // TODO:SendAsync: generate requestId
                Options = cycleTLSRequestOptions
            });

            TaskCompletionSource<CycleTLSResponse> tcs = new TaskCompletionSource<CycleTLSResponse>();
            var cancelSource = new CancellationTokenSource(timeout);
            cancelSource.Token.Register(() => tcs.TrySetException(new TimeoutException($"After {timeout.Seconds} seconds - no response")));

            lock (_lockQueue)
            {
                RequstQueue.Enqueue((cycleTLSRequestOptions, tcs));
                if (!isQueueSendRunning)
                {
                    isQueueSendRunning = true;
                    QueueSendAsync();
                }
            }

            return tcs.Task;
        }
    
        private async Task QueueSendAsync()
        {
            //if (WebSocketClient == null)
            //{
            //    throw new InvalidOperationException("For some reason WebSocket client is not initialized. You should not see this exception");
            //}

            //// Wait max 1500 milliseconds while server or client restarts
            //int attempts = 0;
            //while (!WebSocketClient.IsAlive && attempts < 15)
            //{
            //    await Task.Delay(100);
            //    attempts++;
            //}

            //if (!WebSocketClient.IsAlive)
            //{
            //    // return error
            //}

            //string requestJson;
            //TaskCompletionSource<CycleTLSResponse> requestTcs;
            //lock (_lockQueue)
            //{
            //    (requestJson, requestTcs) = RequstQueue.Dequeue();
            //}

            //ConcurrentQueue<string> q = new ConcurrentQueue<string>();
            //q.
            //WebSocketClient.SendAsync(requestJson, (isResponded))
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
	    public string OrderAsProvided { get; set; } = ""; //`json:"orderAsProvided"`
    }
}