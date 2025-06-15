using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using AAYInvisionaryTTSPlayer.Models;
using AAYInvisionaryTTSPlayer.Services.FallbackTtsService;
using AAYInvisionaryTTSPlayer.Services.PlayerService;
using AAYInvisionaryTTSPlayer.Utilities;
using ChatterboxTTSNet;
using SFML.Audio;

namespace AAYInvisionaryTTSPlayer.Services.ErrorHandler;

public class PythonErrorHandler(IPlayer player, IFallbackTtsService fallbackTts) : IErrorHandler
{
    public async Task<bool> ErrorPlayer(int index, string optionalText = "")
    {
        switch (index)
        {
            case 1:
                await HandleTtsInitializationErrorAsync(new Exception(optionalText));
                return true;
            default:
                return false;
        }
    }
    
    // This method handles an unknown error
    public async Task HandleUnexpectedErrorAsync(Exception ex)
    {
        Console.WriteLine($"--- UNEXPECTED ERROR: {ex.Message} ---");

        // 1. Play a generic, pre-recorded "something went wrong" message.
        await PlayErrorAsync("unexpected_error.ogg"); // You'll need to record this.

        // 2. Use the fallback service to speak the actual exception message!
        await fallbackTts.SpeakAsync(ex.Message, player);
    }
    
    /// <summary>
    /// The main entry point for handling a TTS initialization failure.
    /// It speaks an initial error and then runs an audible diagnostic check.
    /// </summary>
    public async Task HandleTtsInitializationErrorAsync(Exception ex)
    {
        Console.WriteLine($"--- TTS INIT FAILED: {ex.Message} ---");
        await PlayErrorAsync("python_backend_error.ogg");

        bool setupIsCorrect = await CheckPythonBackendAsync();

        if (setupIsCorrect)
        {
            await PlayErrorAsync("backend_other_issue.ogg");
        }
    }

    /// <summary>
    /// Performs a series of checks on the Python environment, playing audio feedback at each step.
    /// </summary>
    /// <returns>True if all checks pass, otherwise false.</returns>
    private async Task<bool> CheckPythonBackendAsync()
    {
        await PlayErrorAsync("python_check_start.ogg");

        // Step 1: Check if 'python' or 'python3' executable exists and is runnable.
        if (!await RunProcessAsync("python3", "--version") && !await RunProcessAsync("python", "--version"))
        {
            await PlayErrorAsync("python_not_found.ogg");
            return false;
        }
        await PlayErrorAsync("python_found.ogg");

        // Step 2: Check if the .venv folder exists.
        string venvPath = Path.Combine(Environment.CurrentDirectory, ".venv");
        if (!Directory.Exists(venvPath))
        {
            await PlayErrorAsync("venv_missing.ogg");
            return false;
        }
        await PlayErrorAsync("venv_found.ogg");

        // Step 3: Check if a key package is installed in the venv.
        string pipPath = Path.Combine(venvPath, "bin", "pip"); // For Linux/macOS
        if (OperatingSystem.IsWindows())
        {
            pipPath = Path.Combine(venvPath, "Scripts", "pip.exe");
        }

        if (!await RunProcessAsync(pipPath, "show chatterbox-tts"))
        {
            await PlayErrorAsync("packages_missing.ogg");
            return false;
        }
        await PlayErrorAsync("packages_found.ogg");

        return true;
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
            AudioBuffer = new SFML.Audio.SoundBuffer(EmbeddedFetcher.ExtractResource(fileName))
        });

        // Wait for the prompt to start playing.
        await Task.Delay(100); 

        // Wait for the prompt to finish playing without blocking the thread.
        while (player.GetPlayStatus() == SoundStatus.Playing)
        {
            await Task.Delay(100);
        }
    }

    /// <summary>
    /// Helper method to run a process and check if it executed successfully.
    /// </summary>
    private async Task<bool> RunProcessAsync(string fileName, string arguments)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };

        try
        {
            process.Start();
            await process.WaitForExitAsync();
            return process.ExitCode == 0;
        }
        catch (Exception)
        {
            return false; // Process failed to start (e.g., command not found)
        }
    }
}