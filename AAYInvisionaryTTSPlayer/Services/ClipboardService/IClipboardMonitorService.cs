#nullable enable
using System;
using System.Threading;

namespace AAYInvisionaryTTSPlayer.Services.ClipboardService;

public interface IClipboardMonitorService
{
    // This property will be our on/off switch
    bool IsEnabled { get; set; }

    event Action<string>? ClipboardTextChanged;
    void StartMonitoring(CancellationToken token);
}