#nullable enable

namespace AAYInvisionaryTTSPlayer.Models;

public class UserSettings
{
    public string SelectedVoice { get; set; } = "Custom";
    public bool AutoRead { get; set; } = false;
    public float Volume { get; set; } = 100.0f;
    public float Rate { get; set; } = 1.0f;
    public string? ChosenBackend { get; set; } = "Python";
}