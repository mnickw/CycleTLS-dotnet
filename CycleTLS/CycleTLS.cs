using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
    // TODO: Debug StartServer, StartClient and everything else
    // TODO: Dispose
    // TODO: logs

    // TODO: Documentation
    // TODO: explain in comments why do we need queue and dictionary
    // TODO: Simple cookies and headers
    // TODO: Ask why websockets, not just usual http
    public class CycleTLSClient
    {
        private readonly ILogger<CycleTLSClient> _logger;

        public TimeSpan DefaultTimeOut { get; set; } = TimeSpan.FromSeconds(100);

        private WebSocket WebSocketClient { get; set; } = null;
        private Process GoServer { get; set; } = null;


        private object _lockQueue = new object();

        private bool isQueueSendRunning = false;

        private Queue<(CycleTLSRequest Request, TaskCompletionSource<CycleTLSResponse> RequestTCS)> RequstQueue { get; set; }
            = new Queue<(CycleTLSRequest Request, TaskCompletionSource<CycleTLSResponse> RequestTCS)>();

        private ConcurrentDictionary<string, TaskCompletionSource<CycleTLSResponse>> SentRequests { get; set; }
            = new ConcurrentDictionary<string, TaskCompletionSource<CycleTLSResponse>>();

        private object _lockRequestCount = new object();
        private int RequestCount { get; set; } = 0;

        public CycleTLSRequestOptions DefaultRequestOptions { get; } = new CycleTLSRequestOptions()
        {
            Ja3 = "771,4865-4867-4866-49195-49199-52393-52392-49196-49200-49162-49161-49171-49172-51-57-47-53-10,0-23-65281-10-11-35-16-5-51-43-13-45-28-21,29-23-24-25-256-257,0",
            UserAgent = "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/101.0.4951.54 Safari/537.36",

            Body = "",
            Cookies = new List<Cookie>(),
            DisableRedirect = "",
            HeaderOrder = new List<string>(),
            Headers = new Dictionary<string, string>(),
            Method = "",
            OrderAsProvided = "",
            Proxy = "",
            Timeout = "",
            Url = ""
        };

        public CycleTLSClient(ILogger<CycleTLSClient> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Creates and runs server with source CycleTLS library and WebSocket client.
        /// </summary>
        /// <param name="port">Port used by server.</param>
        /// <exception cref="InvalidOperationException">Server already initialized.</exception>
        /// <exception cref="PlatformNotSupportedException">Not supported platform.</exception>
        public void InitializeServerAndClient(int port = 9119)
        {
            if (GoServer != null || DoesServerAlreadyRun(port))
            {
                throw new InvalidOperationException("Server already initialized.");
            }

            string executableFilename = "";
            try
            {
                executableFilename = GetExecutableFilename();
            }
            catch (PlatformNotSupportedException)
            {
                _logger.LogError("Operating system not supported.");
                throw;
            }

            StartServer(executableFilename, port);

            StartClient(port);
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
            throw new PlatformNotSupportedException("Not supported platform.");
        }

        private void StartServer(string filename, int port)
        {
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
                    //_logger.LogError($"Error from CycleTLSClient: requestId:{requestId} error:{error}");
                }
                else
                {
                    _logger.LogError($"Server received error data (please open an issue https://github.com/Danny-Dasilva/CycleTLS/issues/new/choose " +
                        $"or https://github.com/mnickw/CycleTLS-dotnet/issues): {ea.Data}");
                    try
                    {
                        GoServer.Kill();
                    }
                    finally
                    {
                        GoServer.Dispose();
                    }

                    StartServer(filename, port);
                }
            };
            GoServer.Start();
        }

        private void StartClient(int port)
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
                    requestPair.Value.TrySetException(new Exception("Error in WebSocket connection.", ea.Exception));
                }

                SentRequests.Clear();

                Task.Delay(100).ContinueWith((t) => StartClient(port));
            };

            ws.Connect();

            WebSocketClient = ws;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="httpMethod">The HTTP method.</param>
        /// <param name="url">A string that represents the request Url.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
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
        public async Task<CycleTLSResponse> SendAsync(HttpMethod httpMethod, string url, TimeSpan timeout)
        {
            return await SendAsync(new CycleTLSRequestOptions()
            {
                Url = url,
                Method = httpMethod.Method
            }, timeout);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="httpMethod"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public async Task<CycleTLSResponse> SendAsync(CycleTLSRequestOptions cycleTLSRequestOptions)
        {
            return await SendAsync(cycleTLSRequestOptions, DefaultTimeOut);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cycleTLSRequestOptions"></param>
        /// <returns></returns>
        public Task<CycleTLSResponse> SendAsync(CycleTLSRequestOptions cycleTLSRequestOptions, TimeSpan timeout)
        {
            if (WebSocketClient == null)
            {
                throw new InvalidOperationException("WebSocket client is not initialized.");
            }

            TaskCompletionSource<CycleTLSResponse> tcs = new TaskCompletionSource<CycleTLSResponse>();
            var cancelSource = new CancellationTokenSource(timeout);
            cancelSource.Token.Register(() => tcs.TrySetException(new TimeoutException($"No response after {timeout.Seconds} seconds.")));

            var request = CreateRequest(cycleTLSRequestOptions);

            lock (_lockQueue)
            {
                RequstQueue.Enqueue((request, tcs));
                if (!isQueueSendRunning)
                {
                    isQueueSendRunning = true;
                    QueueSendAsync();
                }
            }

            return tcs.Task;
        }
        
        private CycleTLSRequest CreateRequest(CycleTLSRequestOptions cycleTLSRequestOptions)
        {
            // There's no records in netstandard2.0, so here's copy of options
            var optionsCopy = new CycleTLSRequestOptions();
            foreach (var propertyInfo in typeof(CycleTLSRequestOptions).GetProperties())
            {
                object defaultOption = propertyInfo.GetValue(DefaultRequestOptions);
                object customOption = propertyInfo.GetValue(cycleTLSRequestOptions);
                if (customOption == null)
                    propertyInfo.SetValue(optionsCopy, defaultOption);
                else
                    propertyInfo.SetValue(optionsCopy, customOption);
            }

            int requestIndex;
            lock (_lockRequestCount)
                requestIndex = ++RequestCount;

            var request = new CycleTLSRequest()
            {
                RequestId = $"{requestIndex}:{DateTime.Now}:{optionsCopy.Url}",
                Options = optionsCopy
            };

            return request;
        }

        private async Task QueueSendAsync()
        {
            while (true)
            {
                if (WebSocketClient == null)
                {
                    throw new InvalidOperationException("Critical error. For some reason WebSocket client is not initialized. " +
                        "Probably, you should not see this exception");
                }

                if (!(await ClientRestartCheckDelay())) return;

                CycleTLSRequest request;
                TaskCompletionSource<CycleTLSResponse> requestTCS;
                lock (_lockQueue)
                {
                    if (!RequstQueue.Any())
                    {
                        isQueueSendRunning = false;
                        return;
                    }
                    (request, requestTCS) = RequstQueue.Dequeue();
                }

                SentRequests.TryAdd(request.RequestId, requestTCS);

                var jsonRequestData = JsonSerializer.Serialize(request);

                WebSocketClient.SendAsync(jsonRequestData, (isCompleted) =>
                {
                    if (!isCompleted)
                    {
                        requestTCS.TrySetException(new Exception("Error in WebSocket connection."));
                        SentRequests.TryRemove(request.RequestId, out _);
                    }
                });
            }
        }

        // Returns true if restart was successful and WebSocketClient is alive now, false otherwise
        private async Task<bool> ClientRestartCheckDelay()
        {
            // Wait max 5000 milliseconds while server or client restarts
            int attempts = 0;
            int maxAttempts = 50;
            int delay = 100;
            while (!WebSocketClient.IsAlive && attempts < 50)
            {
                await Task.Delay(delay);
                attempts++;
            }

            if (!WebSocketClient.IsAlive)
            {
                lock (_lockQueue)
                {
                    while (RequstQueue.Any())
                    {
                        RequstQueue.Dequeue().RequestTCS
                            .TrySetException(new Exception($"Critical error. " +
                                $"WebSocket connection was not established after {maxAttempts * delay} milliseconds."));
                    }
                    isQueueSendRunning = false;
                    return false;
                }
            }
            return true;
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
        public string Url { get; set; } = null;
	    public string Method { get; set; } = null;
        public Dictionary<string, string> Headers { get; set; } = null;
	    public string Body { get; set; } = null;
        public string Ja3 { get; set; } = null;
        public string UserAgent { get; set; } = null;
	    public string Proxy { get; set; } = null;
        public List<Cookie> Cookies { get; set; } = null;
        public string Timeout { get; set; } = null;
        public string DisableRedirect { get; set; } = null;
        public List<string> HeaderOrder { get; set; } = null;
	    public string OrderAsProvided { get; set; } = null;
    }
}