using AAYInvisionaryTTSPlayer.Services.ConnectionService;
using AAYInvisionaryTTSPlayer.Services.ErrorHandler;
using AAYInvisionaryTTSPlayer.Services.FallbackTtsService;
using AAYInvisionaryTTSPlayer.Services.PlayerService;
using AAYInvisionaryTTSPlayer.Services.TTSService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit.Abstractions;

namespace AAYInvisionaryTTSPlayer.Tests;

public class PythonBackendTests(ITestOutputHelper output)
{
    // 1. Add a private field to hold the output helper.

    // 2. Add a constructor that accepts the ITestOutputHelper.
    //    xUnit will automatically provide this for you.

    [Fact]
    [Trait("Category", "Integration")]
    public async Task PythonTtsService_CanGenerateSpeech_Successfully()
    {
        // ARRANGE
        output.WriteLine("--- Test Starting: Setting up DI container... ---");
        var services = new ServiceCollection();
        
        var configuration = new ConfigurationBuilder().Build();
        services.AddSingleton<IConfiguration>(configuration);
        
        services.AddSingleton<IErrorHandler, PythonErrorHandler>(); // Using the real Python error handler
        services.AddSingleton<IConnection, PythonConnection>();
        services.AddSingleton<ITtsService, PythonTtsService>();
        
        services.AddSingleton(new Mock<IPlayer>().Object);
        services.AddSingleton(new Mock<IFallbackTtsService>().Object);

        var serviceProvider = services.BuildServiceProvider();
        var ttsService = serviceProvider.GetRequiredService<ITtsService>();
        output.WriteLine("DI setup complete. ITtsService has been resolved.");
        
        output.WriteLine("Calling GenerateSpeechAsync...");
        var result = await ttsService.GenerateSpeechAsync("This is a test.", "closefail.ogg");
        output.WriteLine(result == null ? "Result from TTS Service was NULL." : "Result received from TTS Service.");
        
        Assert.True(result != null, "The TTS Service returned a null result.");
        Assert.Equal("Success", result.MessageType); // Assuming your Python backend sets this
        Assert.NotEmpty(result.AudioBuffer.Samples);
        Assert.True(result.AudioBuffer.SampleRate > 0);
        Assert.NotEmpty(result.WordTimestamps);
        Assert.Equal("This", result.WordTimestamps.First().Word, ignoreCase: true);
        output.WriteLine("--- Test Passed ---");
    }
}