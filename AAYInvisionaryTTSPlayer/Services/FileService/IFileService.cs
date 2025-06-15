#nullable enable
using System.Collections.Generic;
using Avalonia.Platform.Storage;
using SFML.Audio;
using System.Threading.Tasks;

namespace AAYInvisionaryTTSPlayer.Services.FileService
{
    public interface IFileService
    {
        Task<IStorageFile?> SaveFileAsync(IStorageProvider storageProvider);
        
        Task<IReadOnlyList<IStorageFile>> LoadFileAsync(IStorageProvider storageProvider);
    }
}
