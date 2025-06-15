using System;
using System.Threading.Tasks;
using AAYInvisionaryTTSPlayer.Services.PlayerService;

namespace AAYInvisionaryTTSPlayer.Services.ErrorHandler;

public interface IErrorHandler
{
    public Task<bool> ErrorPlayer(int index, string optionalText = "");

    public Task HandleTtsInitializationErrorAsync(Exception ex);
}