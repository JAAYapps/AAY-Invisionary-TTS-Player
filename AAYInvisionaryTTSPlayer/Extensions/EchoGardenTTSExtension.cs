using System;
using System.Buffers;
using System.Collections.Generic;
using AAYInvisionaryTTSPlayer.Models;
using AAYInvisionaryTTSPlayer.Utilities;
using DynamicData;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using ReactiveUI;

namespace AAYInvisionaryTTSPlayer.Extensions
{
    public class EchoGardenTTSExtension
    {
        public class EchoGardenTTSExtensionFormatter : IMessagePackFormatter<TTSMessage.Message>
        {
            private void ReadTimeline(List<TTSMessage.Timeline> timelines, ref MessagePackReader reader)
            {
                int count = reader.ReadArrayHeader();
                for (int i = 0; i < count; i++)
                {
                    int headerCount = reader.ReadMapHeader();
                    TTSMessage.Timeline timeline = new TTSMessage.Timeline();
                    while (!reader.TryReadNil() && headerCount-- > 0)
                    {
                        string header = reader.ReadString();
                        if ("type".Equals(header))
                            timeline.type = reader.ReadString();
                        else if ("text".Equals(header))
                            timeline.text = reader.ReadString();
                        else if ("startTime".Equals(header))
                            timeline.startTime = reader.ReadDouble();
                        else if ("endTime".Equals(header))
                            timeline.endTime = reader.ReadDouble();
                        else if ("timeline".Equals(header) && !reader.IsNil)
                            ReadTimeline(timeline.timeline, ref reader);
                        else
                            reader.Skip();
                    }
                    timelines.Add(timeline);
                }
            }

            private TTSMessage.Audio DecodeAudio(ref MessagePackReader reader)
            {
                int count = reader.ReadMapHeader();
                TTSMessage.Audio audio = new TTSMessage.Audio();
                int channels = 1;
                List<short> audioData = new List<short>();
                while (!reader.TryReadNil() && count-- > 0)
                {
                    string header = reader.ReadString();
                    if ("audioChannels".Equals(header))
                    {
                        channels = reader.ReadArrayHeader();
                        for (int i = 0; i < channels; i++)
                        {
                            ExtensionResult result = reader.ReadExtensionFormat();
                            byte[] data = result.Data.ToArray();
                            // float[] floatArray = new float[data.Length / 4];
                            // Buffer.BlockCopy(data, 0, floatArray, 0, data.Length);
                            short[] sdata = new short[data.Length / 4]; //[floatArray.Length];
                            for (int j = 0; j < data.Length; j += 4)
                            {
                                sdata[j / 4] = (short)Math.Floor(BitConverter.ToSingle(new byte[] { data[j], data[j + 1], data[j + 2] , data[j + 3] } ) * 32767);
                                // sdata[j] = (short)Math.Floor(((float)((data[j] * (8 * 4)) + (data[j + 1] * (8 * 3)) + (data[j + 2] * (8 * 2)) + data[j + 3])) * 32767);
                                // sdata[j] = (short)Math.Floor(floatArray[j] * 32767);
                            }
                            audioData.AddRange(sdata);
                        }
                    }
                    else if ("sampleRate".Equals(header))
                        audio.sampleRate = reader.ReadInt32();
                    else
                        reader.Skip();
                }

                try
                {
                    audio.AudioData.AddRange(ByteManager.ShortArrayToByteArray(audioData.ToArray()));
                    audio.ChannelCount = channels; 
                    audio.sampleRate = audio.sampleRate;
                }
                catch (Exception)
                {
                    audio.AudioData.Add(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 });
                    audio.ChannelCount = 1;
                    audio.sampleRate = 44100;
                    Console.WriteLine($"The conversion failed and had to give a blank. Here is the data that was seen. {audioData.ToArray()}");
                }
                return audio;
            }

            public void PopulateMessage(TTSMessage.Message message, ref MessagePackReader reader)
            {
                // Loop over *all* array elements independently of how many we expect,
                // since if we're serializing an older/newer version of this object it might
                // vary in number of elements that were serialized, but the contract of the formatter
                // is that exactly one data structure must be read, regardless.
                // Alternatively, we could check that the size of the array/map is what we expect
                // and throw if it is not.
                int count = reader.ReadMapHeader();
                // Console.WriteLine("Map Header size for message: " + count);
                while (!reader.TryReadNil() && count-- > 0)
                {
                    string header = reader.ReadString();
                    if ("requestId".Equals(header))
                        message.requestId = reader.ReadString();
                    else if ("messageType".Equals(header))
                        message.messageType = reader.ReadString();
                    else if ("index".Equals(header))
                        message.index = reader.ReadInt32();
                    else if ("total".Equals(header))
                        message.total = reader.ReadInt32();
                    else if ("audio".Equals(header))
                        message.audio = DecodeAudio(ref reader);
                    else if ("timeline".Equals(header))
                        ReadTimeline(message.timeline, ref reader);
                    else if ("transcript".Equals(header))
                        message.transcript = reader.ReadString();
                    else if ("language".Equals(header))
                        message.language = reader.ReadString();
                    else if ("peakDecibelsSoFar".Equals(header))
                        message.peakDecibelsSoFar = reader.ReadDouble();
                    else
                    {
                        // Console.WriteLine(header);
                        reader.Skip();
                    }
                }
            }

            public TTSMessage.Message Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
            {
                // Deserialize and convert the data back to Float32Array
                if (reader.TryReadNil())
                {
                    return null;
                }
                TTSMessage.Message message = new TTSMessage.Message();
                options.Security.DepthStep(ref reader);

                try
                {
                    PopulateMessage(message, ref reader);
                    reader.Depth--;
                    return message;
                }
                catch (Exception e)
                {
                    reader.Depth--;
                    Console.WriteLine(e.ToString());
                    throw;
                }
            }
            
            /// <summary>
            /// This function does not implement a TTSMessage Serialization.
            /// TTSMessage is only an output structure from EchoGarden.
            /// EchoGarden takes in a different input that does not match TTSMessage.
            /// </summary>
            /// <param name="writer"></param>
            /// <param name="value"></param>
            /// <param name="options"></param>
            public void Serialize(ref MessagePackWriter writer, TTSMessage.Message value, MessagePackSerializerOptions options)
            {
                // Purposefully throw an error.
                throw new NotImplementedException("EchoGarden takes in a different input that does not match TTSMessage.");
            }
        }

        // Custom extension handler
        public class EchoGardenTTSExtensionResolver : IFormatterResolver
        {
            public IMessagePackFormatter<T> GetFormatter<T>()
            {
                if (typeof(T) == typeof(TTSMessage.Message))
                {
                    return (IMessagePackFormatter<T>)new EchoGardenTTSExtensionFormatter();
                }
                // Return the default formatter for other types
                return StandardResolver.Instance.GetFormatter<T>();
            }
        }
    }
}
