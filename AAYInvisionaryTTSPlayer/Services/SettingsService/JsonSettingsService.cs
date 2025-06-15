using System;
using System.IO;
using System.Text.Json;
using AAYInvisionaryTTSPlayer.Models;

namespace AAYInvisionaryTTSPlayer.Services.SettingsService
{
    public class JsonSettingsService : ISettingsService
    {
        private readonly string _filePath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
    
        public void Save(UserSettings settings)
        {
            var jsonRoot = new { UserSettings = settings };
            string json = JsonSerializer.Serialize(jsonRoot, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }
    }
}

