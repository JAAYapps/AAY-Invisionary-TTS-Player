#nullable enable
using Avalonia.Input.Platform; 
using System;
using System.Threading.Tasks;

namespace AAYInvisionaryTTSPlayer.Services.ClipboardService;

// Renamed for clarity, as it simulates input before reading the clipboardService.
public class SimulatedClipboardService : IClipboardService
{
    // This field will hold the actual clipboardService service provided by Avalonia.
    private readonly IClipboard platformClipboardService;

    // The constructor now correctly asks for the clipboardService service via DI.
    public SimulatedClipboardService(IClipboard platformClipboard)
    {
        this.platformClipboardService = platformClipboard;
    }

    public async Task<string[]?> GetClipboard()
    {
        // Read the actual text from the system clipboardService.
        string? text = await platformClipboardService.GetTextAsync();
        
        // The text replacements and splitting are to make TTS appear more instant.
        // Long paragraphs have shown delayed responses and TTS engines devolving into gibberish sounds like a toddler.
        if (!string.IsNullOrEmpty(text))
        {
            text = text.Replace(".NET", " dot net")
                       .Replace(".net", " dot net")
                       .Replace("C#", "C Sharp");
        }

        return text?.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
    }
}