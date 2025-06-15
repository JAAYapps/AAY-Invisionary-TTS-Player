using AAYInvisionaryTTSPlayer.Models;

namespace AAYInvisionaryTTSPlayer.Services.SettingsService;

public interface ISettingsService
{
    void Save(UserSettings settings);
}