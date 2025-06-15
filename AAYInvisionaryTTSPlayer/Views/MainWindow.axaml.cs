#nullable enable
using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform;
using Avalonia.Threading;

namespace AAYInvisionaryTTSPlayer.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        // GotFocusEvent.Raised.Subscribe(Focused);
    }

    //private void Focused((object, RoutedEventArgs) eventArgs)
    //{
    //    FocusManager?.ClearFocus();
    //}

    #region Mouse Window Drag
    private bool mouseDownForWindowMoving = false;
    private PointerPoint originalPoint;

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!mouseDownForWindowMoving) return;

        PointerPoint currentPoint = e.GetCurrentPoint(this);
        Position = new PixelPoint(Position.X + (int)(currentPoint.Position.X - originalPoint.Position.X),
            Position.Y + (int)(currentPoint.Position.Y - originalPoint.Position.Y));
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (WindowState == WindowState.Maximized || WindowState == WindowState.FullScreen)
            return;
        mouseDownForWindowMoving = true;
        originalPoint = e.GetCurrentPoint(this);
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        mouseDownForWindowMoving = false;
    }
    #endregion

    #region Minumize
    private bool isMinimized = false;
    private PixelPoint savedPosition;
    
    private void Minimizing_OnClick(object? sender, RoutedEventArgs e)
    {
        // Toggle the state
        isMinimized = !isMinimized;

        if (isMinimized)
        {
            savedPosition = this.Position;
            
            // Set the new minimized state
            // NOTE: A height below 65 was found to cause a native SIGSEGV crash
            // on Linux with KDE Plasma 6. A value of 65 is stable and avoids this bug.
            // May happen with other Desktop Environments
            // Windows doesn't have this render issue.
            RootPanel.Width = 150;
            RootPanel.Height = 65;
            
            this.SizeToContent = SizeToContent.WidthAndHeight;

            // 4. Use the Dispatcher to move the window AFTER the layout has updated.
            //    This gives Avalonia a moment to calculate the new size before we move it.
            Dispatcher.UIThread.Post(() =>
            {
                var screens = this.Screens;
                var currentScreen = screens.ScreenFromWindow(this) ?? screens.Primary;
                if (currentScreen is null) return;
                
                int newX = currentScreen.WorkingArea.X + 5;
                int newY = currentScreen.WorkingArea.Y + currentScreen.WorkingArea.Height - (int)this.Bounds.Height - 5;
                
                this.Position = new PixelPoint(newX, newY);
            }, DispatcherPriority.Background);
        }
        else
        {
            // --- RESTORE LOGIC ---
            
            // 1. IMPORTANT: Turn off auto-sizing so we can set the size manually.
            this.SizeToContent = SizeToContent.Manual;
            
            // 2. Restore the original size of the window and its content
            this.Width = 310;
            this.Height = 180;
            RootPanel.Width = Double.NaN; // 'NaN' resets the content size to stretch
            RootPanel.Height = Double.NaN;

            // 3. Restore the window's position
            this.Position = savedPosition;
        }
    }
    /*bool isMinimized = false;
    PixelPoint savedPosition; // Use a PixelPoint struct to save the position

    private void Minimizing_OnClick(object? sender, RoutedEventArgs e)
    {
        // We post the entire logic to the dispatcher. This schedules it to run
        // right after the current UI event (the mouse click) has finished processing.
        // This avoids the race condition in the native window manager.
        Dispatcher.UIThread.Post(() =>
        {
            var screens = this.Screens;
            var currentScreen = screens.ScreenFromWindow(this) ?? screens.Primary;
            if (currentScreen is null) return;

            // Toggle the state
            isMinimized = !isMinimized;

            if (isMinimized)
            {
                // Store the current position before moving
                savedPosition = this.Position;
                Console.WriteLine(savedPosition.X);
                Console.WriteLine(savedPosition.Y);
                
                // Set the new minimized state
                this.Height = 60;
                this.Width = 150;
                
                // Use WorkingArea to avoid placing the window under a taskbar or dock
                int newX = currentScreen.WorkingArea.X + 5;
                int newY = currentScreen.WorkingArea.Y + currentScreen.WorkingArea.Height - 60 - 5; // Position 5px from bottom
                
                this.Position = new PixelPoint(newX, newY);
            }
            else
            {
                // Restore the previous state
                this.Height = 200;
                this.Width = 310;
                this.Position = savedPosition;
            }
        }, DispatcherPriority.Background); // Using Background priority is safest for UI manipulation
    }*/
    /*bool minumizing = false;
    int savedx = 0;
    int savedy = 0;
    private void Minimizing_OnClick(object? sender, RoutedEventArgs e)
    {
        minumizing = !minumizing;
        var screens = Screens.All;
        // double width = screen.Bounds.Width;
        Screen screen = screens[0];
        int height = screen.Bounds.Height;
        if (minumizing)
        {
            savedx = this.Position.X;
            savedy = this.Position.Y;
            Console.WriteLine(savedx);
            Console.WriteLine(savedy);
            this.Height = 60;
            this.Width = 150;
            this.Position = new PixelPoint(5, height - 80);
        }
        else
        {
            this.Height = 150;
            this.Width = 420;
            if (savedx < 0)
                savedx = 10;
            if (savedy < 0)
                savedy = 10;
            this.Position = new PixelPoint(savedx, savedy);
        }
    }*/
    #endregion

}