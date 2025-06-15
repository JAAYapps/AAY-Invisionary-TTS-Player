#nullable enable
using MessagePack;
using MessagePack.Resolvers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using AAYInvisionaryTTSPlayer.Extensions;
using AAYInvisionaryTTSPlayer.Models;
using AAYInvisionaryTTSPlayer.Services.ErrorHandler;
using ChatterboxTTSNet;
using SFML.Audio;
using static AAYInvisionaryTTSPlayer.Extensions.EchoGardenTTSExtension;

namespace AAYInvisionaryTTSPlayer.Services.ConnectionService
{
    public class WebConnection(IErrorHandler errorHandler) : IConnection
    {
        private readonly ClientWebSocket webSocket = new ClientWebSocket();
        private readonly IErrorHandler handler = errorHandler;
        private readonly ConcurrentQueue<TTSResult> receivedMessages = new();
        
        public async Task<bool> Connect()
        {
            if (webSocket.State != WebSocketState.Open)
            {
                await webSocket.ConnectAsync(new Uri("ws://localhost:45054"), CancellationToken.None);
                Console.WriteLine("Connected.");
            }
            return webSocket.State == WebSocketState.Open;
        }

        public async Task<bool> Disconnect()
        {
            // await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "TTS Request Processed", CancellationToken.None);
            await webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "TTS Request Processed", CancellationToken.None);
            // Console.WriteLine(webSocket.CloseStatus.ToString());
            return webSocket.State != WebSocketState.Open;
        }

        private async Task ListenForMessagesAsync(CancellationToken token)
        {
            var buffer = new ArraySegment<byte>(new byte[8192]);

            while (webSocket.State == WebSocketState.Open && !token.IsCancellationRequested)
            {
                try
                {
                    using var memoryStream = new MemoryStream();
                    WebSocketReceiveResult response;
                    do
                    {
                        response = await webSocket.ReceiveAsync(buffer, token);
                        memoryStream.Write(buffer.Array!, buffer.Offset, response.Count);
                    } while (!response.EndOfMessage);

                    memoryStream.Seek(0, SeekOrigin.Begin);
                    
                    // --- We have received a full message, now process it ---
                    ProcessReceivedStream(memoryStream);
                }
                catch (WebSocketException)
                {
                    // Connection was likely closed by the server. Stop listening.
                    Console.WriteLine("WebSocket connection closed. Stopping listener.");
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in listener: {ex.Message}");
                    // Keep listening unless the socket is closed.
                }
            }
        }

        private void ProcessReceivedStream(MemoryStream stream)
        {
            try
            {
                var options = MessagePackSerializerOptions.Standard.WithResolver(CompositeResolver.Create(
                    new[] { new EchoGardenTTSExtension.EchoGardenTTSExtensionFormatter() },
                    new[] { new EchoGardenTTSExtension.EchoGardenTTSExtensionResolver() }
                ));

                // Loop in case EchoGarden sends multiple objects in one message
                while (stream.Position < stream.Length)
                {
                    var complexMessage = MessagePackSerializer.Deserialize<TTSMessage.Message>(stream, options);
                    if (complexMessage == null) continue;

                    var flatTimestamps = new List<WordTimestamp>();
                    FlattenTimeline(complexMessage.timeline, flatTimestamps);

                    var result = new TTSResult
                    {
                        AudioBuffer = complexMessage.audio.audioChannels.FirstOrDefault() ?? new SoundBuffer(new short[0], 1, 44100),
                        WordTimestamps = flatTimestamps,
                        MessageType = complexMessage.messageType
                    };

                    receivedMessages.Enqueue(result);
                }
            }
            catch (Exception e)
            {
                _ = handler.ErrorPlayer(0, $"Failed to process message from backend. Error: {e.Message}");
            }
        }
        
        public async Task<TTSResult> Received()
        {
            /*using var memoryStream = new MemoryStream();
            WebSocketReceiveResult response;
            var buffer = new ArraySegment<byte>(new byte[8192]);
            do
            {
                if (webSocket.State != WebSocketState.Open)
                {
                    // Connection closed unexpectedly.
                    await handler.ErrorPlayer(0, "The connection to the TTS backend was lost.");
                    return new TTSResult { MessageType = "Failed" };
                }
                
                response = await webSocket.ReceiveAsync(buffer, CancellationToken.None);
                    
                memoryStream.Write(buffer.Array!, buffer.Offset, response.Count);
            } while (!response.EndOfMessage);
            
            // Rewind the stream to the beginning so we can read from it
            memoryStream.Seek(0, SeekOrigin.Begin);
            var allAudioSamples = new List<short>();
            var allTimestamps = new List<WordTimestamp>();
            var finalMessageType = "Processing";
            uint sampleRate = 44100; // Default sample rate
            
            try
            {
                var options = MessagePackSerializerOptions.Standard
                    .WithResolver(CompositeResolver.Create(
                        new[] { new EchoGardenTTSExtensionFormatter() },
                        new[] { new EchoGardenTTSExtensionResolver() }
                    ));
            
                // Keep reading from the stream as long as there are bytes left.
                while (memoryStream.Position < memoryStream.Length)
                {
                    // Deserialize one message at a time from the stream.
                    var message = MessagePackSerializer.Deserialize<TTSMessage.Message>(memoryStream, options);

                    if (message?.audio?.audioChannels.FirstOrDefault() is { } audioBuffer)
                    {
                        allAudioSamples.AddRange(audioBuffer.Samples);
                        sampleRate = audioBuffer.SampleRate;
                    }

                    if (message?.timeline != null)
                    {
                        FlattenTimeline(message.timeline, allTimestamps);
                    }
            
                    // The last message type in the stream will be the final one.
                    if(message != null) finalMessageType = message.messageType;
                }

                // 3. Combine the results into a single TTSResult.
                return new TTSResult
                {
                    AudioBuffer = new SoundBuffer(allAudioSamples.ToArray(), 1, sampleRate),
                    WordTimestamps = allTimestamps,
                    MessageType = finalMessageType
                };
            }
            catch (Exception e)
            {
                await handler.ErrorPlayer(0, $"Failed to process the message from the backend. Error: {e.Message}");
                return new TTSResult { MessageType = "Failed" };
            }*/
            
            bool endOfAllMessages = false;
            TTSMessage.Message? message = null;
            while (!endOfAllMessages)
            {
                bool endMessage = false;
                List<byte> fullBuffer = new List<byte>();
                // using var memoryStream = new MemoryStream(); // <- Your stream
                while (webSocket.State == WebSocketState.Open && !endMessage)
                {
                    //byte[] buffer = new byte[1024];
                    //int offset = 0;
                    //int packet = 1024;
                    //ArraySegment<byte> byteReceived = new ArraySegment<byte>(buffer, offset, packet);
                    var buffer = new ArraySegment<byte>(new byte[8192]); // <- your array Segment
                    //WebSocketReceiveResult response = await webSocket.ReceiveAsync(byteReceived, CancellationToken.None);
                    WebSocketReceiveResult response = await webSocket.ReceiveAsync(buffer, CancellationToken.None); // <- your response
                    // memoryStream.Write(buffer.Array!, buffer.Offset, response.Count); // <- your segment add
                    fullBuffer.AddRange(buffer);
                    endMessage = response.EndOfMessage;
                }
                
                // memoryStream.Seek(0, SeekOrigin.Begin); // <- your seek to beginning
                
                try
                {
                    var options = MessagePackSerializerOptions.Standard
                        .WithResolver(CompositeResolver.Create(
                            new[] { new EchoGardenTTSExtension.EchoGardenTTSExtensionFormatter() },
                            new[] { new EchoGardenTTSExtension.EchoGardenTTSExtensionResolver() }
                        ));
                    // Console.WriteLine("Success with " + MessagePackSerializer.ConvertToJson(fullBuffer.ToArray()));
                    message = MessagePackSerializer.Deserialize<TTSMessage.Message>(fullBuffer.ToArray(), options); // <- Mine failed with --- SENT ERROR: Failed to deserialize AAYInvisionaryTTSPlayer.Models.TTSMessage+Message value. --- but the retry system in viewmodel run this again and it actually plays on second attempt.
                    // message = MessagePackSerializer.Deserialize<TTSMessage.Message>(memoryStream.ToArray(), options); // <- Using it gets the --- SENT ERROR: Failed to deserialize AAYInvisionaryTTSPlayer.Models.TTSMessage+Message value. --- on every try

                    // Console.WriteLine(message.index + " " + message.total);
                    endOfAllMessages = message.total == 0;
                }
                catch (Exception e)
                {
                    await this.handler.ErrorPlayer(0, e.Message);
                    message = new TTSMessage.Message { messageType = "Failed", transcript = e.Message };
                }
            }
            
            var flatWordTimestamps = new List<WordTimestamp>();
            if (message != null)
            {
                FlattenTimeline(message.timeline, flatWordTimestamps);

                return new TTSResult
                {
                    AudioBuffer = message.audio.audioChannels.FirstOrDefault() ??
                                  new SoundBuffer(new short[0], 1, 44100),
                    WordTimestamps = flatWordTimestamps,
                    MessageType = message.messageType,
                };
            }

            return new TTSResult() { MessageType = "empty" };
        }

        public async Task Send(string message, string ttsVoice)
        {
            if (webSocket.State != WebSocketState.Open)
                await Connect();
            if (webSocket.State == WebSocketState.Open)
            {
                string randomCryptoString = GenerateRandomCryptoString();
                var messageData = new { messageType = "SynthesisRequest", requestId = randomCryptoString, input = message, options = new { engine = "vits", voice = ttsVoice, speed = 1.50} };
                byte[] serializer = MessagePackSerializer.Serialize(messageData);
                await webSocket.SendAsync(new ArraySegment<byte>(serializer), WebSocketMessageType.Binary, true, CancellationToken.None);
            }
        }

        private static string GenerateRandomCryptoString(int length = 32)
        {
            byte[] randomBytes = RandomNumberGenerator.GetBytes(length);
            return BitConverter.ToString(randomBytes).Replace("-", "");
        }
        
        private void FlattenTimeline(List<TTSMessage.Timeline> nestedTimeline, List<WordTimestamp> flatList)
        {
            foreach (var item in nestedTimeline)
            {
                if (item.type.Equals("word", StringComparison.OrdinalIgnoreCase))
                {
                    // We only care about words, so add them to our flat list.
                    flatList.Add(new WordTimestamp(item.text, item.startTime, item.endTime));
                }

                // If this timeline item has its own children, process them recursively.
                if (item.timeline != null && item.timeline.Any())
                {
                    FlattenTimeline(item.timeline, flatList);
                }
            }
        }
    }
}
