#nullable enable
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using System;
using AAYInvisionaryTTSPlayer.Models;
using AAYInvisionaryTTSPlayer.Services.ClipboardService;
using AAYInvisionaryTTSPlayer.Services.ConnectionService;
using AAYInvisionaryTTSPlayer.Services.ErrorHandler;
using AAYInvisionaryTTSPlayer.Services.FallbackTtsService;
using AAYInvisionaryTTSPlayer.Services.FileService;
using AAYInvisionaryTTSPlayer.Services.InitializerService;
using AAYInvisionaryTTSPlayer.Services.PlayerService;
using AAYInvisionaryTTSPlayer.Services.SettingsService;
using AAYInvisionaryTTSPlayer.Services.TTSService;
using AAYInvisionaryTTSPlayer.Utilities;
using AAYInvisionaryTTSPlayer.ViewModels;
using AAYInvisionaryTTSPlayer.Views;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Microsoft.Extensions.Configuration;
using SFML.Audio;

namespace AAYInvisionaryTTSPlayer;

public class App : Application
{
    /// <summary>
    /// Gets the current instance of the application.
    /// </summary>
    public new static App Current => (App)Application.Current!;

    /// <summary>
    /// Gets the service provider for dependency injection.
    /// </summary>
    public IServiceProvider Services { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (Design.IsDesignMode)
        {
            base.OnFrameworkInitializationCompleted();
            return;
        }
        
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();
            
        var services = new ServiceCollection();

        // Register the configuration object itself and the strongly-typed settings
        services.AddSingleton<IConfiguration>(configuration);
        services.Configure<UserSettings>(configuration.GetSection("UserSettings"));
        
        services.AddTransient<EchoGardenPlayer>();
        services.AddTransient<ChatterboxPlayer>();
        
        var chosenBackend = configuration.GetValue<string>("UserSettings:ChosenBackend");
        
        services.AddSingleton<IFallbackTtsService, FallbackTtsService>(); 
        // IConnection is a Proxy Strategy for the backends.
        if (chosenBackend == "EchoGarden")
        {
            services.AddSingleton<IErrorHandler, EchoGardenErrorHandler>();
            // WebConnection is the HTTP client side that connects to the EchoGarden beackend.
            services.AddSingleton<IConnection, WebConnection>();
            services.AddSingleton<ITtsService, EchoGardenTtsService>();
        }
        else if (chosenBackend == "Python")
        {
            services.AddSingleton<IErrorHandler, PythonErrorHandler>();
            // Acts as a local connection to Python
            services.AddSingleton<IConnection, PythonConnection>();
            services.AddSingleton<ITtsService, PythonTtsService>();
        }
        
        services.AddSingleton<IPlayer>(provider =>
        {
            return chosenBackend switch
            {
                "EchoGarden" => provider.GetRequiredService<EchoGardenPlayer>(),
                "Python" => provider.GetRequiredService<ChatterboxPlayer>(),
                // If the config is invalid or missing, we throw a specific exception.
                _ => throw new BackendNotConfiguredException("No valid TTS backend was specified in appsettings.json.")
            };
        });
        
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // MUST RETRIEVE THE CLIPBOARD FROM MAIN WINDOW!!!
            var mainWindow = new MainWindow();
            var platformClipboard = mainWindow.Clipboard;
            if (platformClipboard != null) services.AddSingleton<IClipboard>(platformClipboard);
            services.AddSingleton<IClipboardMonitorService, ClipboardMonitorService>();
            // Register all the application's services.
            services.AddSingleton<ISettingsService, JsonSettingsService>();
            services.AddSingleton<IClipboardService, SimulatedClipboardService>();
            services.AddSingleton<IFileService, FileService>();

            // In App.axaml.cs, add these registrations
            services.AddTransient<EchoGardenInitializer>();
            services.AddTransient<PythonInitializer>();
            services.AddSingleton<IBackendInitializer>(provider =>
            {
                var chosenBackend = provider.GetRequiredService<IConfiguration>()
                    .GetValue<string>("UserSettings:ChosenBackend");
                return chosenBackend switch
                {
                    "EchoGarden" => provider.GetRequiredService<EchoGardenInitializer>(),
                    "Python" => provider.GetRequiredService<PythonInitializer>(),
                    _ => provider.GetRequiredService<PythonInitializer>() // Default to Python or throw
                };
            });
            
            // Register ViewModels
            services.AddTransient<MainWindowViewModel>();

            try
            {
                Services = services.BuildServiceProvider();
            
                // Get the ViewModel
                var viewModel = Services.GetRequiredService<MainWindowViewModel>();

                // Set the DataContext and assign the SAME instance as the main window
                mainWindow.DataContext = viewModel;
                desktop.MainWindow = mainWindow;
                
                var initializer = Services.GetRequiredService<IBackendInitializer>();
                var handler = Services.GetRequiredService<IErrorHandler>();
                initializer.InitializeAsync();
            }
            catch (Exception ex) when (ex.GetBaseException() is BackendNotConfiguredException)
            {
                // The DI container failed to build.
                Console.WriteLine(ex.GetBaseException().Message);
                
                // Play the audible error message using a temporary, manually created player.
                PlayEmergencyErrorAndShutdown("no_backend_configured.ogg");
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// Plays a critical error sound when the main DI container fails to build, then exits.
    /// </summary>
    private void PlayEmergencyErrorAndShutdown(string fileName)
    {
        try
        {
            var audioData = EmbeddedFetcher.ExtractResource(fileName);
            if (audioData == null) return;

            // Create a temporary player instance just for this message.
            using var soundBuffer = new SoundBuffer(audioData);
            using var sound = new Sound(soundBuffer);
            sound.Play();
            
            // Wait for the sound to finish before exiting.
            while (sound.Status == SoundStatus.Playing)
            {
                System.Threading.Thread.Sleep(100);
            }
        }
        finally
        {
            // Ensure the application closes.
            Environment.Exit(1);
        }
    }
    
    // A custom exception class to make our error handling specific and clean.
    private class BackendNotConfiguredException(string message) : Exception(message);
}