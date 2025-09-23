using System.Diagnostics;
using CSnakes.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Runtime.CompilerServices;
using System.Text.Json;
using CSnakes.Runtime.Python;

namespace ChatterboxTTSNet;

public static class ChatterboxTTSFactory
{
    private static IPythonEnvironment? _env;
    private static PyObject model = null!;
    private static ITtsBackend module = null!;
    
    public static void Initialize()
    {
        var builder = Host.CreateApplicationBuilder();
        var home = Path.Join(Environment.CurrentDirectory);

        // The path to the 'bin' directory inside your .venv
        var pythonBinPath = Path.Join(home, ".venv");

        builder.Services
            .WithPython()
            .WithHome(home)
            .FromRedistributable("3.11")
            .WithVirtualEnvironment(pythonBinPath)
            .WithPipInstaller("requirements.txt");
        Console.WriteLine("Path:" + builder.Environment.ContentRootPath); 
        var app = builder.Build();

        _env = app.Services.GetRequiredService<IPythonEnvironment>();
        if (_env == null)
        {
            throw new InvalidOperationException("ChatterboxTTSFactory has not been initialized.");
        }

        Console.WriteLine($"Using Python Version: {_env.Version}");
        module = _env.TtsBackend();
        model = module.LoadModel();
    }

    public static (Int64 sampleRate, short[]? audioSamples, List<WordTimestamp> timeStamp) GenerateAudio(string text, string wavInput,
        double exaggeration = 0.5,
        double temperature = 0.8,
        double cfgWeight = 0.8,
        double minP = 0.05,
        double topP = 1.0,
        double repetitionPenalty = 1.2,
        int seed = 0)
    {
        using PyObject textObj = PyObject.From(text);
        using PyObject wavInputObj = PyObject.From(wavInput);
        using PyObject exaggerationObj = PyObject.From(exaggeration);
        using PyObject temperatureObj = PyObject.From(temperature);
        using PyObject seedNumObj = PyObject.From(seed);
        using PyObject cfgwObj = PyObject.From(cfgWeight);
        using PyObject minPObj = PyObject.From(minP);
        using PyObject topPObj = PyObject.From(topP);
        using PyObject repetitionPenaltyObj = PyObject.From(repetitionPenalty);
        
        ITuple result = module.Generate(model, textObj, wavInputObj, exaggerationObj, temperatureObj, seedNumObj, cfgwObj, minPObj, topPObj, repetitionPenaltyObj);

        // --- Correctly Unpack the Python Tuple using the Buffer Protocol ---

        /*if (!(result.As<ITuple>().GetType() == typeof(ITuple)) || result.As<ITuple>().Length != 2)
        {
            throw new InvalidOperationException("Python function did not return a valid (sample_rate, audio_data) tuple.");
        }*/
        
        var sampleRate = (Int64)(result[0] ?? 0);

        IPyBuffer? pyAudioNumpyArray = ((IPyBuffer?)result[1]);

        Console.WriteLine(result.GetType());
        //Console.WriteLine("Type: " + sampleRate.GetType() + " " + sampleRate);
        //Console.WriteLine("Type: " + pyAudioNumpyArray.GetPythonType() + " " + pyAudioNumpyArray);
        // Get the numpy array's dtype name as a string
        //using PyObject dtype = ((PyObject)pyAudioNumpyArray).GetAttr("dtype");
        //using PyObject dtypeName = dtype.GetAttr("name");
        string numpyDtype = "float32";//dtypeName.As<string>();
        Console.WriteLine($"Python returned a NumPy array with dtype: {numpyDtype}");
        
        short[]? audioSamples;
        // Handle the data based on its actual type
        if (numpyDtype == "float32")
        {
            Console.WriteLine("Data is float32. Converting to int16 for SFML...");
            Debug.Assert(pyAudioNumpyArray != null, nameof(pyAudioNumpyArray) + " != null");
            ReadOnlySpan<float> floatSamplesEnumerable = pyAudioNumpyArray.AsFloatReadOnlySpan();
            float[] floatSamples = floatSamplesEnumerable.ToArray();
            audioSamples = new short[floatSamples.Length];
            for (int i = 0; i < floatSamples.Length; i++)
            {
                audioSamples[i] = (short)(floatSamples[i] * short.MaxValue);
            }
        }
        else if (numpyDtype == "int16")
        {
            Console.WriteLine("Data is already int16. Converting directly...");
            audioSamples = ((PyObject)pyAudioNumpyArray!)?.As<short[]>();
        }
        else
        {
            throw new NotSupportedException($"Unsupported NumPy dtype '{numpyDtype}' for audio conversion.");
        }
        
        Console.WriteLine($"Successfully created {audioSamples?.Length} audio samples at {sampleRate} Hz.");

        return (sampleRate, audioSamples, GetTimestampsFromBuffer(audioSamples, sampleRate, text));
    }
    
    // NEW METHOD: Calls the Python script with the in-memory buffer.
    public static List<WordTimestamp> GetTimestampsFromBuffer(short[]? audioSamples, long sampleRate, string text)
    {
        if (_env == null)
        {
            throw new InvalidOperationException("ChatterboxTTSFactory has not been initialized.");
        }

        var module = _env.Aligner(); 
        
        List<byte> audioSamplesToLong = new List<byte>();
        for (int i = 0; i < audioSamples?.Length; i++)
            audioSamplesToLong.AddRange(FromShort(audioSamples[i]));
        
        string jsonResult = module.GetWordTimestampsFromBuffer(audioSamplesToLong.ToArray(), sampleRate, text);
        
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var timestamps = JsonSerializer.Deserialize<List<WordTimestamp>>(jsonResult, options);

        return timestamps ?? new List<WordTimestamp>();
    }

    static byte[] FromShort(short number)
    {
        byte byte2 = (byte)(number >> 8);
        byte byte1 = (byte)(number & 255);
        return [byte1, byte2];
    }

    public static void Uninitialize()
    {
        model.Close();
        model.Dispose();
        module.Dispose();
        _env?.Dispose();
    }
}