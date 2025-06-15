#nullable enable
using SFML.Audio;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AAYInvisionaryTTSPlayer.Models;
using AAYInvisionaryTTSPlayer.Services.PlayerService;
using AAYInvisionaryTTSPlayer.Utilities;
using ChatterboxTTSNet;

namespace AAYInvisionaryTTSPlayer.Services.FallbackTtsService;

public class FallbackTtsService : IFallbackTtsService
{
    // This Regex splits text into words, ignoring most punctuation.
    private static readonly Regex WordSplitter = new(@"\b[\w']+\b", RegexOptions.Compiled);

    public async Task SpeakAsync(string text, IPlayer player)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        var words = WordSplitter.Matches(text.ToLower());

        foreach (Match match in words)
        {
            var word = match.Value;
            if (word == "0")
                word = "zero";
            else if (word == "1")
                word = "one";
            else if (word == "2")
                word = "two";
            else if (word == "3")
                word = "three";
            else if (word == "4")
                word = "four";
            else if (word == "5")
                word = "five";
            else if (word == "6")
                word = "six";
            else if (word == "7")
                word = "seven";
            else if (word == "8")
                word = "eight";
            else if (word == "9")
                word = "nine";
            var wordAudio = EmbeddedFetcher.ExtractResource($"FallbackWords.{word}.ogg");   // Automatically gets the correct path to Assets.

            if (wordAudio is { Length: > 4 })
            {
                // If we found the whole word, play it.
                player.AddToQueue(new TTSResult{AudioBuffer = new SoundBuffer(wordAudio), MessageType = "Internal", WordTimestamps = new List<WordTimestamp>()});
            }
            else
            {
                // If the word isn't in our database, spell it out letter by letter.
                await SpeakAsSpellingAsync(word, player);
            }

            /*// Wait for the word to finish playing plus a small pause
            await Task.Delay(50);
            while (player.GetPlayStatus() == SoundStatus.Playing)
            {
                await Task.Delay(100);
            }
            await Task.Delay(10); */// Artificial pause between words for clarity
        }
    }

    private async Task SpeakAsSpellingAsync(string text, IPlayer player)
    {
        Console.WriteLine($"[FallbackTTS] Word not found: '{text}'. Spelling it out.");

        // Optional: Play a "spelling" prompt to alert the user.
        var spellingPromptAudio = EmbeddedFetcher.ExtractResource("FallbackWords.spelling.ogg");
        if (spellingPromptAudio != null)
        {
            player.AddToQueue(new TTSResult{ AudioBuffer = new SoundBuffer(spellingPromptAudio), MessageType = "Internal", WordTimestamps = new List<WordTimestamp>()});
            await Task.Delay(200);
            while (player.GetPlayStatus() == SoundStatus.Playing) { await Task.Delay(100); }
        }

        foreach (char c in text)
        {
            var letter = c.ToString().ToLower();
            if (letter == "0")
                letter = "zero";
            else if (letter == "1")
                letter = "one";
            else if (letter == "2")
                letter = "two";
            else if (letter == "3")
                letter = "three";
            else if (letter == "4")
                letter = "four";
            else if (letter == "5")
                letter = "five";
            else if (letter == "6")
                letter = "six";
            else if (letter == "7")
                letter = "seven";
            else if (letter == "8")
                letter = "eight";
            else if (letter == "9")
                letter = "nine";
            
            var letterAudio = EmbeddedFetcher.ExtractResource($"FallbackWords.{letter}.ogg");

            if (letterAudio != null)
            {
                player.AddToQueue(new TTSResult{ AudioBuffer = new SoundBuffer(letterAudio), MessageType = "Internal", WordTimestamps = new List<WordTimestamp>()});
                // await Task.Delay(20); // Shorter delay between letters
                while (player.GetPlayStatus() == SoundStatus.Playing) { await Task.Delay(50); }
            }
        }
    }
}