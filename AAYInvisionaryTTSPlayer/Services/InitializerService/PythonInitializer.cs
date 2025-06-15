using System;
using System.Threading.Tasks;

namespace AAYInvisionaryTTSPlayer.Services.InitializerService;

public class PythonInitializer : IBackendInitializer
{
    public Task InitializeAsync()
    {
        // The Python backend is initialized on its first use in PythonConnection, so this is intentionally left with just a print.
        Console.WriteLine("Python backend selected. Initialization will occur on first TTS request.");
        return Task.CompletedTask;
    }
}