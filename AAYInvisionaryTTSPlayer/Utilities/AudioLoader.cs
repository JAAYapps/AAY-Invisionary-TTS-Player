using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NAudio.Wave;
using NLayer;
using NVorbis;

namespace AAYInvisionaryTTSPlayer.Utilities;

public class AudioLoader
{
    public static (short[] Samples, int SampleRate) LoadAudioFromFile(string filePath)
    {
        if (!File.Exists(filePath)) 
            throw new FileNotFoundException($"Audio file not found: {filePath}");

        using var stream = File.OpenRead(filePath);
        var extension = Path.GetExtension(filePath).ToLower();

        return extension switch
        {
            ".ogg" => LoadOgg(stream),
            ".mp3" => LoadMp3(stream),
            ".wav" => LoadWav(stream),
            _ => throw new NotSupportedException($"Unsupported format: {extension}")
        };
    }

    private static (short[], int) LoadOgg(Stream stream)
    {
        // 'true' means we don't own the stream (optional, depends on your usage)
        using var vorbis = new VorbisReader(stream, true);

        // Read all samples
        // Note: vorbis.TotalSamples is per-channel, so we multiply by channels
        var floatSamples = new float[vorbis.TotalSamples * vorbis.Channels];
        vorbis.ReadSamples(floatSamples, 0, floatSamples.Length);

        // FIX: Mix down Stereo to Mono
        if (vorbis.Channels == 2)
        {
            var monoSamples = new float[floatSamples.Length / 2];

            for (int i = 0; i < floatSamples.Length; i += 2)
            {
                float left = floatSamples[i];
                float right = (i + 1 < floatSamples.Length) ? floatSamples[i + 1] : 0;

                // Average L + R
                monoSamples[i / 2] = (left + right) * 0.5f;
            }

            // Return the new mono array
            return (FloatsToShorts(monoSamples), vorbis.SampleRate);
        }

        // If already Mono (or unsupported surround), just return as-is
        return (FloatsToShorts(floatSamples), vorbis.SampleRate);
    }

    private static (short[], int) LoadMp3(Stream stream)
    {
        using var mpegFile = new MpegFile(stream);
    
        // Read all samples (floats)
        var allSamples = new List<float>();
        var readBuffer = new float[mpegFile.SampleRate * mpegFile.Channels]; 
        int readCount;

        while ((readCount = mpegFile.ReadSamples(readBuffer, 0, readBuffer.Length)) > 0)
        {
            allSamples.AddRange(readBuffer.Take(readCount));
        }

        // FIX: If the file is Stereo (2 channels), mix it down to Mono
        if (mpegFile.Channels == 2)
        {
            var monoSamples = new List<float>(allSamples.Count / 2);
        
            // Loop through pairs (Left, Right)
            for (int i = 0; i < allSamples.Count; i += 2)
            {
                float left = allSamples[i];
                // Ensure we don't go out of bounds if there is an odd sample at the end
                float right = (i + 1 < allSamples.Count) ? allSamples[i + 1] : 0;

                // Average them to create a Mono sample
                monoSamples.Add((left + right) * 0.5f);
            }
        
            // Replace the original list with our new Mono list
            allSamples = monoSamples;
        }
        // If it's more than 2 channels (rare), you could add logic here, 
        // but typically we just handle Stereo -> Mono.

        return (FloatsToShorts(allSamples.ToArray()), mpegFile.SampleRate);
    }

    private static (short[], int) LoadWav(Stream stream)
    {
        using var reader = new WaveFileReader(stream);
    
        // Convert whatever format the WAV is (16-bit, 24-bit, Float) into standard IEEE Floats
        var sampleProvider = reader.ToSampleProvider();
    
        int channels = sampleProvider.WaveFormat.Channels;
        int sampleRate = sampleProvider.WaveFormat.SampleRate;

        // Read all samples into a float buffer
        var allSamples = new List<float>();
        var buffer = new float[sampleRate * channels]; 
        int read;
    
        while ((read = sampleProvider.Read(buffer, 0, buffer.Length)) > 0)
        {
            allSamples.AddRange(buffer.Take(read));
        }

        // FIX: Stereo to Mono Mixdown
        if (channels == 2)
        {
            var monoSamples = new List<float>(allSamples.Count / 2);
        
            for (int i = 0; i < allSamples.Count; i += 2)
            {
                float left = allSamples[i];
                float right = (i + 1 < allSamples.Count) ? allSamples[i + 1] : 0;
            
                monoSamples.Add((left + right) * 0.5f);
            }
        
            return (FloatsToShorts(monoSamples.ToArray()), sampleRate);
        }

        // Return as-is if already mono
        return (FloatsToShorts(allSamples.ToArray()), sampleRate);
        /*using var reader = new WaveFileReader(stream);
        if (reader.WaveFormat.Encoding == WaveFormatEncoding.Pcm && reader.WaveFormat.BitsPerSample == 16)
        {
            // Optimization: If already 16-bit PCM, read raw bytes and convert
            byte[] buffer = new byte[reader.Length];
            reader.Read(buffer, 0, buffer.Length);
            short[] shorts = new short[buffer.Length / 2];
            System.Buffer.BlockCopy(buffer, 0, shorts, 0, buffer.Length);
            return (shorts, reader.WaveFormat.SampleRate);
        }
        // Otherwise, convert via SampleProvider (handles Float, 24-bit, etc.)
        return ReadProviderToShorts(reader.ToSampleProvider());*/
    }

    private static (short[], int) ReadProviderToShorts(ISampleProvider provider)
    {
        var allFloats = new List<float>();
        var buffer = new float[provider.WaveFormat.SampleRate * provider.WaveFormat.Channels];
        int read;
        while ((read = provider.Read(buffer, 0, buffer.Length)) > 0)
        {
            allFloats.AddRange(buffer.Take(read));
        }
        
        return (FloatsToShorts(allFloats.ToArray()), provider.WaveFormat.SampleRate);
    }

    private static short[] FloatsToShorts(float[] floats)
    {
        var shorts = new short[floats.Length];
        for (int i = 0; i < floats.Length; i++)
        {
            // Clamp and convert
            float temp = floats[i] * 32768f;
            if (temp > 32767f) temp = 32767f;
            else if (temp < -32768f) temp = -32768f;
            shorts[i] = (short)temp;
        }
        return shorts;
    }
}