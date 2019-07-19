using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Ceen;
using Ceen.Httpd;
using Ceen.Httpd.Handler;
using Ceen.Httpd.Logging;
using Ceen.Mvc;

namespace Unreal_Dumping_Agent.Http
{
    public class HttpManager : IDisposable
    {
        private readonly CancellationTokenSource _tcs = new CancellationTokenSource();
        private Task _listenTask;

        public bool Working { get; set; }
        public int Port { get; set; }
        public string SitePath { get; set; }

        public void Start(string sitePath, int port)
        {
            Port = port;
            SitePath = sitePath;

            // API REST
            var config = new ServerConfig()
                .AddLogger(new CLFStdOut())
                .AddRoute(new FileHandler(SitePath))
                .AddRoute(typeof(ApiUdaController).Assembly //Load all types in assembly
                    .ToRoute(new ControllerRouterConfig(
                            // Set as default controller
                            typeof(ApiUdaController)
                        )
                    )
                );

            // HttpServer
            _listenTask = HttpServer.ListenAsync(
                new IPEndPoint(IPAddress.Any, Port),
                false,
                config,
                _tcs.Token
            );

            Working = true;
        }

        public void Stop()
        {
            if (!Working)
                return;

            _tcs.Cancel();
            _listenTask.Wait();
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
