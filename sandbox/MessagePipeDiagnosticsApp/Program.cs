using System;
using MessagePipe;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using MessagePipe.Interprocess;
using System.IO;

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
        static void Main(string[] args)
        {
            using var tempPath = new DisposableTempPath(true);
            var services = new ServiceCollection();
            services.AddMessagePipe()
                .AddMessagePipeTcpInterprocessUds(tempPath.TempPath)
                ;
            using var listener = new ActivityListener();
            ActivitySamplingResult Sample(ref ActivityCreationOptions<ActivityContext> options)
            {
                return ActivitySamplingResult.AllData;
            }
            listener.Sample += Sample;
            listener.ShouldListenTo += (src) =>
            {
                if(src.Name.StartsWith("MessagePipe"))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            };
        }
    }
}
