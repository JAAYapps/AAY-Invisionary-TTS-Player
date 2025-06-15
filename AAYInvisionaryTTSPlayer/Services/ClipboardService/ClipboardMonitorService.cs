#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Input.Platform;
using Avalonia.Threading;

namespace AAYInvisionaryTTSPlayer.Services.ClipboardService;

public class ClipboardMonitorService(IClipboard clipboard) : IClipboardMonitorService
{
    private readonly IClipboard _clipboard = clipboard;
    private string _lastClipboardText = string.Empty;

    // The public property to control the monitoring state. Default to on.
    public bool IsEnabled { get; set; } = true;

    public event Action<string>? ClipboardTextChanged;

    public void StartMonitoring(CancellationToken token)
    {
        Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                if (IsEnabled)
                {
                    try
                    {
                        string? currentText = await Dispatcher.UIThread.InvokeAsync(_clipboard.GetTextAsync);

                        if (!string.IsNullOrEmpty(currentText) && currentText != _lastClipboardText)
                        {
                            _lastClipboardText = currentText;
                            ClipboardTextChanged?.Invoke(currentText);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ClipboardMonitor] Error checking clipboard: {ex.Message}");
                    }
                }
                
                await Task.Delay(500, token);
            }
        }, token);
    }
}