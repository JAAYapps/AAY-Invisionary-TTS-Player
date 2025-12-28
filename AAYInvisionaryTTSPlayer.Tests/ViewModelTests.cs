using AAYInvisionaryTTSPlayer.Models;
using AAYInvisionaryTTSPlayer.Services.ClipboardService;
using AAYInvisionaryTTSPlayer.Services.ErrorHandler;
using AAYInvisionaryTTSPlayer.Services.FileService;
using AAYInvisionaryTTSPlayer.Services.PlayerService;
using AAYInvisionaryTTSPlayer.Services.SettingsService;
using AAYInvisionaryTTSPlayer.Services.TTSService;
using AAYInvisionaryTTSPlayer.ViewModels;
using Microsoft.Extensions.Options;
using Moq;

namespace AAYInvisionaryTTSPlayer.Tests;

public class ViewModelTests
{
    [Fact]
    public async Task PlayTtsCommand_WhenExecuted_CallsTtsServiceAndQueuesAudio()
    {
        var mockTtsService = new Mock<ITtsService>();
        var mockPlayer = new Mock<IPlayer>();
        var mockClipboard = new Mock<IClipboardService>();
        
        var mockSettings = Options.Create(new UserSettings());
        var mockFileService = new Mock<IFileService>();
        var mockSettingsService = new Mock<ISettingsService>();
        var mockErrorHandler = new Mock<IErrorHandler>();
        var mockClipboardMonitor = new Mock<IClipboardMonitorService>();
        
        mockClipboard.Setup(c => c.GetClipboard()).ReturnsAsync(new[] { "hello world" });
        
        var fakeTtsResult = new TTSResult { MessageType = "Stream", AudioBuffer = new byte[2], ChannelCount = 1, BitRate = 24000 };
        mockTtsService.Setup(t => t.GenerateSpeechAsync(It.IsAny<string>(), It.IsAny<string>()))
                      .ReturnsAsync(fakeTtsResult);
        
        mockPlayer.Setup(p => p.GetPlayStatus()).Returns(IPlayer.SoundStatus.Stopped);
        
        var viewModel = new MainWindowViewModel(
            mockPlayer.Object,
            mockClipboard.Object,
            mockTtsService.Object,
            mockFileService.Object,
            mockSettingsService.Object,
            mockSettings,
            mockErrorHandler.Object,
            mockClipboardMonitor.Object
        );
        
        await viewModel.PlayTtsCommand.ExecuteAsync(null);
        
        mockTtsService.Verify(t => t.GenerateSpeechAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        
        mockPlayer.Verify(p => p.AddToQueue(fakeTtsResult), Times.Once);
    }
}