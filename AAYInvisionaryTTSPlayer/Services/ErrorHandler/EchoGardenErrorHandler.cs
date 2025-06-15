using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AAYInvisionaryTTSPlayer.Models;
using AAYInvisionaryTTSPlayer.Services.FallbackTtsService;
using AAYInvisionaryTTSPlayer.Services.PlayerService;
using AAYInvisionaryTTSPlayer.Utilities;
using SFML.Audio;

namespace AAYInvisionaryTTSPlayer.Services.ErrorHandler
{
    public class EchoGardenErrorHandler(IPlayer player, IFallbackTtsService fallbackTts) : IErrorHandler
    {
        public async Task<bool> ErrorPlayer(int index, string optionalText = "")
        {
            switch (index)
            {
                case 0:
                    return SentErrorToEchoGardenForVoiceReading(optionalText);
                case 1:
                    await HandleTtsInitializationErrorAsync(new Exception(optionalText));
                    return true;
                default:
                    return false;
            }
        }

        // This method handles an unknown error
        private async Task HandleUnexpectedErrorAsync(Exception ex)
        {
            Console.WriteLine($"--- UNEXPECTED ERROR: {ex.Message} ---");
            
            await PlayErrorAsync("something_went_wrong.wav");
            
            await fallbackTts.SpeakAsync(ex.Message, player);
        }
        
        public async Task HandleTtsInitializationErrorAsync(Exception ex)
        {
            await PlayErrorAsync("EchogardenError.ogg");
            if (await CheckInstallation() && ex != null)
                SentErrorToEchoGardenForVoiceReading(ex.Message); // If installation of EchoGarden is working, use the command version of EchoGarden to give the error message.
        }

        private async Task<bool> CheckInstallation()
        {
            Process process = new Process();
            process.StartInfo = new ProcessStartInfo()
            {
                FileName = "node",
                Arguments = "--version",
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Normal,
                UseShellExecute = false,
                ErrorDialog = true,
                RedirectStandardOutput = true
            };
            try
            {
                process.Start();
                Console.WriteLine(await process.StandardOutput.ReadLineAsync());
            }
            catch (Exception)
            {
                await PlayErrorAsync("nonodeinstall.ogg");
                return false;
            }
            process = new Process();
            process.StartInfo = new ProcessStartInfo()
            {
                FileName = "echogarden",
                Arguments = "speak testing",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            try
            {
                process.Start();
                await process.WaitForExitAsync();
                if (process.ExitCode == 0)
                {
                    // The command was found and ran successfully.
                    await PlayErrorAsync("EchogardenSuccess.ogg");
                    await Task.Delay(1000);
                    await PlayErrorAsync("Different issue.wav");
                    return true;
                }
                else
                {
                    // The command was found but returned an error code.
                    string errorOutput = await process.StandardError.ReadToEndAsync();
                    Console.WriteLine($"Echogarden failed with exit code {process.ExitCode}: {errorOutput}");
                    await PlayErrorAsync("EchogardenFail.ogg");
                    return true; // The installation is there but some other error happened.
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Echogarden failed with exception {e.Message}");
                await PlayErrorAsync("EchogardenFail.ogg");
                await HandleUnexpectedErrorAsync(e);
                return false;
            }
        }

        private bool SentErrorToEchoGardenForVoiceReading(string text)
        {
            Console.WriteLine($"--- SENT ERROR: {text} ---");
            try
            {
                Process process = new Process();
                process.StartInfo = new ProcessStartInfo()
                {
                    FileName = "echogarden",
                    Arguments = "speak \"" + text + "\" --engine=vits --voice=en_US-kusal-medium",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    ErrorDialog = true
                };
                process.Start();
                while (!process.HasExited)
                    Thread.Sleep(1000);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to send. The error was " + text);
                _ = HandleUnexpectedErrorAsync(new Exception("Failed to send. The error was " + text + e.Message));
                return false;
            }
        }

        /// <summary>
        /// Asynchronously plays a pre-recorded error message from an embedded resource.
        /// </summary>
        private async Task PlayErrorAsync(string fileName)
        {
            var audioData = EmbeddedFetcher.ExtractResource(fileName);
            if (audioData == null) return;

            var soundBuffer = new SoundBuffer(audioData);
            player.AddToQueue(new TTSResult
            {
                AudioBuffer = soundBuffer
            });

            // Wait for the prompt to start playing.
            await Task.Delay(100); 

            // Wait for the prompt to finish playing without blocking the thread.
            while (player.GetPlayStatus() is SoundStatus.Playing or SoundStatus.Paused)
            {
                await Task.Delay(100);
            }
        }
    }
}
