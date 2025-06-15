using System.Threading.Tasks;
using AAYInvisionaryTTSPlayer.Models;


namespace AAYInvisionaryTTSPlayer.Services.ConnectionService
{
    public interface IConnection
    {
        public Task<bool> Connect();

        public Task<bool> Disconnect();

        Task Send(string message, string TTSVoice);

        Task<TTSResult> Received();
    }
}
