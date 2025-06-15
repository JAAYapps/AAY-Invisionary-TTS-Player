#nullable enable
using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AAYInvisionaryTTSPlayer.Models;
using AAYInvisionaryTTSPlayer.Services.ConnectionService;
using AAYInvisionaryTTSPlayer.Services.ErrorHandler;

namespace AAYInvisionaryTTSPlayer.Services.TTSService;

// This service knows how to handle the send/receive logic for EchoGarden.
public class EchoGardenTtsService(IConnection connection, IErrorHandler errorHandler) : ITtsService
{
    Process process = new Process();

    bool error;

    public Task StartBackend()
    {
        try
        {
            process.StartInfo = new ProcessStartInfo() 
            { 
                FileName = "echogarden", 
                Arguments = "serve", 
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,   
            };
            process.Start();
        }
        catch (Exception)
        {
            error = true;
        }
        return Task.CompletedTask;
    }

    public bool IsBackendSendingErrors()
    {
        return error;
    }

    public bool IsBackendRunning()
    {
        try
        {
            return !process.HasExited;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task StopBackend()
    {
        try
        {
            process.Kill();
        }
        catch (Exception)
        {
             await errorHandler.ErrorPlayer(2);
        }
    }
    
    public async Task<TTSResult?> GenerateSpeechAsync(string text, string voiceId)
    {
        // All the complexity of the send/receive workflow is hidden here.
        if (!await connection.Connect())
        {
            return null; // Or throw an exception to be handled by the ViewModel's error handler
        }

        await connection.Send(text.Replace(".NET", " dot net").Replace("C#", "C Sharp"), voiceId);
        return await connection.Received();
    }
}