using System.Threading.Tasks;
using AAYInvisionaryTTSPlayer.Services.PlayerService;

namespace AAYInvisionaryTTSPlayer.Services.FallbackTtsService;

public interface IFallbackTtsService
{
    /// <summary>
    /// Speaks the given text using the pre-recorded word database.
    /// </summary>
    Task SpeakAsync(string text, IPlayer player);
}