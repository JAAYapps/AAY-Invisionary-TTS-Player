#nullable enable
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AAYInvisionaryTTSPlayer.Models;
using AAYInvisionaryTTSPlayer.Services.ConnectionService;

namespace AAYInvisionaryTTSPlayer.Services.TTSService;

// This service knows how to handle the queuing logic for Python.
public class PythonTtsService(IConnection connection) : ITtsService
{
    private bool isBackendRunning;
    private bool isBackendSendingErrors;
    
    public async Task StartBackend()
    {
        Console.WriteLine("Python Backend starting...");
        if (!await connection.Connect())
            isBackendSendingErrors = true;
        else
            isBackendRunning = true;
    }

    public bool IsBackendSendingErrors()
    {
        return isBackendSendingErrors;
    }

    public bool IsBackendRunning()
    {
        return isBackendRunning;
    }

    public async Task StopBackend()
    {
        await connection.Disconnect();
        Console.WriteLine("Python Backend is inactive while there are no requests.");
    }

    public async Task<TTSResult> GenerateSpeechAsync(string text, string voiceId)
    {
        if (!isBackendRunning)
            await StartBackend();
        await connection.Send(text.Replace(".NET", " dot net").Replace("C#", "C Sharp"), voiceId);
        return await connection.Received();
    }
}