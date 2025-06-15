using System.Threading.Tasks;
using AAYInvisionaryTTSPlayer.Services.ErrorHandler;

namespace AAYInvisionaryTTSPlayer.Services.InitializerService;

public interface IBackendInitializer
{
    Task InitializeAsync();
}