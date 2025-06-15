using System.Threading.Tasks;
using AAYInvisionaryTTSPlayer.Services.ErrorHandler;
using AAYInvisionaryTTSPlayer.Services.TTSService;
using Microsoft.Extensions.DependencyInjection;

namespace AAYInvisionaryTTSPlayer.Services.InitializerService;

public class EchoGardenInitializer(IErrorHandler handler) : IBackendInitializer
{
    public async Task InitializeAsync()
    {
        var echogarden = App.Current.Services.GetRequiredService<ITtsService>();
        await echogarden.StartBackend();
        if (!echogarden.IsBackendRunning() && echogarden.IsBackendSendingErrors())
        {
            await handler.ErrorPlayer(1);
        }
    }
}