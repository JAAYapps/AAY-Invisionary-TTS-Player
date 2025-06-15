using System;
using System.IO;
using System.Reflection;
using SkiaSharp;

namespace AAYInvisionaryTTSPlayer.Utilities;

public static class AssetGrabber
{
    public static Stream GetAssetStream(string filename)
    {
        var info = Assembly.GetExecutingAssembly().GetName();  
        var name = info.Name;  
        return Assembly.GetExecutingAssembly().GetManifestResourceStream($"{name}.Assets.{filename}")!;
    }
    
    // Helper methods (placeholders)
    public static SKBitmap LoadImage(string assetPath ,string name)
    {
        try
        {
            return SKBitmap.Decode(GetAssetStream($"{assetPath}.{name}")); // Adjust path as needed
        }
        catch (Exception)
        {
            //Console.WriteLine(e);
            return SKBitmap.Decode(GetAssetStream($"Assets\\AAYicon.ico")); // Adjust path as needed
        }
    }

    public static Stream LoadSound(string assetPath, string name)
    {
        try
        {
            return GetAssetStream($"{assetPath}.{name}"); // Adjust path as needed
        }
        catch (Exception e)
        {
            Console.WriteLine(e + "\n No Sound Found");
            return Stream.Null;
        }
    }
    
    public static void PlaySound(string name) => Console.WriteLine($"Playing {name}"); // Replace with actual audio library
}