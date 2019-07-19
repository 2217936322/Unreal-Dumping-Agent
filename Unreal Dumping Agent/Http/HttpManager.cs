using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Ceen;
using Ceen.Httpd;
using Ceen.Httpd.Handler;
using Ceen.Httpd.Logging;
using Ceen.Mvc;
using Newtonsoft.Json;

namespace Unreal_Dumping_Agent.Http
{
    public class TimeOfDayHandler : IHttpModule
    {
        public async Task<bool> HandleAsync(IHttpContext context)
        {
            context.Response.SetNonCacheable();
            await context.Response.WriteAllJsonAsync(JsonConvert.SerializeObject(new { time = DateTime.Now.TimeOfDay }));
            return true;
        }
    }

    public class HttpManager : IDisposable
    {
        private readonly CancellationTokenSource _tcs = new CancellationTokenSource();
        private Task _listenTask;

        public bool Working { get; set; }
        public int Port { get; set; }

        public void Start(int port)
        {
            Port = port;

            var asm = typeof(ApiExampleController).Assembly;
            var route = asm.ToRoute(new ControllerRouterConfig(typeof(ApiExampleController)));

            // API REST
            var config = new ServerConfig()
                .AddLogger(new CLFStdOut())
                .AddRoute("/", new TimeOfDayHandler())
                .AddRoute(route);

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
