using System;
using MessagePipe;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using MessagePipe.Interprocess;
using System.IO;
using OpenTelemetry.Exporter;
using OpenTelemetry;
using OpenTelemetry.Trace;

namespace MessagePipeDiagnosticsApp
{
    public class MyAsyncHandler : IAsyncRequestHandler<int, byte[]>
    {
        public async ValueTask<byte[]> InvokeAsync(int request, CancellationToken cancellationToken = default)
        {
            await Task.Delay(1);
            if (request == -1)
            {
                throw new Exception("NO -1");
            }
            else
            {
                return new byte[request];
            }
        }
    }
    class DisposableTempPath: IDisposable
    {
        public string TempPath { get; private set; }
        public DisposableTempPath(bool ensureRemove)
        {
            TempPath = Path.GetTempFileName();
            if (ensureRemove)
            {
                File.Delete(TempPath);
            }
        }
        public void Dispose()
        {
            if(File.Exists(TempPath))
            {
                File.Delete(TempPath);
            }
        }
    }
    class Program
    {
        static async Task Main(string[] args)
        {
            using var tempPath = new DisposableTempPath(true);
            var services = new ServiceCollection();
            using var tracer = Sdk.CreateTracerProviderBuilder()
                .AddSource("MessagePipe.Interprocess.Workers.TcpWorker")
                .AddConsoleExporter()
                .Build()
                ;
            var provider = services.AddMessagePipe()
                .AddMessagePipeTcpInterprocessUds(tempPath.TempPath, options =>
                {
                    options.HostAsServer = true;
                })
                .AddAsyncRequestHandler<MyAsyncHandler>()
                .BuildServiceProvider()
                ;
            var requestor = provider.GetService<IRemoteRequestHandler<int, byte[]>>() ?? throw new ArgumentNullException("provider");
            await requestor.InvokeAsync(1);
        }
    }
}
