using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Nui;
using static Nui.Input;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Numerics;

namespace TestBlit.WebUI
{
    internal class Server
    {
        private HttpListener _httpListener;

        public WebBlitter Blitter;
        public Input Input { get; set; }

        public void Start()
        {
            _httpListener = new HttpListener();

            _httpListener.Prefixes.Add("http://localhost:4444/");
            _httpListener.Start();

            string[] resourceNames = Assembly.GetExecutingAssembly().GetManifestResourceNames();
            var stream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream(resourceNames.First(p => p.Contains("fontBlack.png")));
            using (var memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);
                var bytes = memoryStream.ToArray();
                _fontImageBytes = bytes;
                //_imageString = "data:image/png;base64," + Convert.ToBase64String(bytes);
            }

            stream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream(resourceNames.First(p => p.Contains("Page.html")));
            using (var memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);
                var bytes = memoryStream.ToArray();
                _pageBytes = bytes;
                //var utf8 = Encoding.UTF8;
                //_pageString = utf8.GetString(bytes);
                //_pageString = _pageString.Replace("\"font\"", "\"" + _imageString + "\"");
            }

            Receive();
        }

        public void Stop()
        {
            _httpListener.Stop();
        }

        private void Receive()
        {
            _httpListener.BeginGetContext(ListenerCallback, _httpListener);
        }

        private void ListenerCallback(IAsyncResult result)
        {
            if (_httpListener.IsListening)
            {
                var context = _httpListener.EndGetContext(result);
                var request = context.Request;

                if (request.IsWebSocketRequest)
                {
                    ProcessRequest(context);
                }
                else if (request.HttpMethod == "GET")
                {
                    var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
                    while (dir.Name != "TestBlit")
                    {
                        dir = dir.Parent;
                    }

                    if (request.RawUrl == "/fontBlack")
                    {
                        var response = context.Response;
                        response.StatusCode = (int)HttpStatusCode.OK;
                        response.ContentType = "image/png";

#if DEBUG
                        response.OutputStream.Write(File.ReadAllBytes(dir.FullName + "/Resources/fontBlack.png"));
#else
                        response.OutputStream.Write(_fontImageBytes);
#endif
                        response.OutputStream.Close();
                    }
                    if (request.RawUrl == "/fontWhite")
                    {
                        var response = context.Response;
                        response.StatusCode = (int)HttpStatusCode.OK;
                        response.ContentType = "image/png";

#if DEBUG
                        response.OutputStream.Write(File.ReadAllBytes(dir.FullName + "/Resources/font.png"));
#else
                        response.OutputStream.Write(_fontImageBytes);
#endif
                        response.OutputStream.Close();
                    }
                    else
                    {
                        var response = context.Response;
                        response.StatusCode = (int)HttpStatusCode.OK;
                        response.ContentType = "text/html";

                        //string str = _pageString;
                        //var utf8 = Encoding.UTF8;
                        //byte[] utfBytes = utf8.GetBytes(str);
#if DEBUG
                        response.OutputStream.Write(File.ReadAllBytes(dir.FullName + "/Resources/Page.html"));
#else
                        response.OutputStream.Write(_pageBytes);
#endif
                        response.OutputStream.Close();
                    }
                }


                Receive();
            }
        }

        private int count = 0;
        private string _imageString;
        private string _pageString;

        //### Accepting WebSocket connections
        // Calling `AcceptWebSocketAsync` on the `HttpListenerContext` will accept the WebSocket connection, sending the required 101 response to the client
        // and return an instance of `WebSocketContext`. This class captures relevant information available at the time of the request and is a read-only 
        // type - you cannot perform any actual IO operations such as sending or receiving using the `WebSocketContext`. These operations can be 
        // performed by accessing the `System.Net.WebSocket` instance via the `WebSocketContext.WebSocket` property.
        // 

        class Connection
        {
            public WebSocketContext WebSocketContext;
            public int Id;
            public byte[] _lastSend;
        }

        private ConcurrentDictionary<int, Connection> _connections = new();
        private byte[] _fontImageBytes;
        private byte[] _pageBytes;

        private async void ProcessRequest(HttpListenerContext listenerContext)
        {
            WebSocketContext webSocketContext = null;
            Connection connection = null;
            try
            {
                // When calling `AcceptWebSocketAsync` the negotiated subprotocol must be specified. This sample assumes that no subprotocol 
                // was requested. 
                webSocketContext = await listenerContext.AcceptWebSocketAsync(subProtocol: null);
                Interlocked.Increment(ref count);
                connection = new Connection()
                {
                    Id = count,
                    WebSocketContext = webSocketContext
                };
                _connections.TryAdd(count, connection);
                Console.WriteLine("Processed: {0}", count);

            }
            catch (Exception e)
            {
                // The upgrade process failed somehow. For simplicity lets assume it was a failure on the part of the server and indicate this using 500.
                listenerContext.Response.StatusCode = 500;
                listenerContext.Response.Close();
                Console.WriteLine("Exception: {0}", e);
                return;
            }

            WebSocket webSocket = connection.WebSocketContext.WebSocket;

            try
            {
                //### Receiving
                // Define a receive buffer to hold data received on the WebSocket connection. The buffer will be reused as we only need to hold on to the data
                // long enough to send it back to the sender.
                byte[] receiveBuffer = new byte[1024];

                // While the WebSocket connection remains open run a simple loop that receives data and sends it back.
                while (webSocket.State == WebSocketState.Open)
                {
                    // The first step is to begin a receive operation on the WebSocket. `ReceiveAsync` takes two parameters:
                    //
                    // * An `ArraySegment` to write the received data to. 
                    // * A cancellation token. In this example we are not using any timeouts so we use `CancellationToken.None`.
                    //
                    // `ReceiveAsync` returns a `Task<WebSocketReceiveResult>`. The `WebSocketReceiveResult` provides information on the receive operation that was just 
                    // completed, such as:                
                    //
                    // * `WebSocketReceiveResult.MessageType` - What type of data was received and written to the provided buffer. Was it binary, utf8, or a close message?                
                    // * `WebSocketReceiveResult.Count` - How many bytes were read?                
                    // * `WebSocketReceiveResult.EndOfMessage` - Have we finished reading the data for this message or is there more coming?
                    WebSocketReceiveResult receiveResult =
                        await webSocket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), CancellationToken.None);

                    // The WebSocket protocol defines a close handshake that allows a party to send a close frame when they wish to gracefully shut down the connection.
                    // The party on the other end can complete the close handshake by sending back a close frame.
                    //
                    // If we received a close frame then lets participate in the handshake by sending a close frame back. This is achieved by calling `CloseAsync`. 
                    // `CloseAsync` will also terminate the underlying TCP connection once the close handshake is complete.
                    //
                    // The WebSocket protocol defines different status codes that can be sent as part of a close frame and also allows a close message to be sent. 
                    // If we are just responding to the client's request to close we can just use `WebSocketCloseStatus.NormalClosure` and omit the close message.
                    if (receiveResult.MessageType == WebSocketMessageType.Close)
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                    }
                    // This echo server can't handle text frames so if we receive any we close the connection with an appropriate status code and message.
                    else if (receiveResult.MessageType == WebSocketMessageType.Text)
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.InvalidMessageType, "Cannot accept text frame",
                            CancellationToken.None);
                    }
                    // Otherwise we must have received binary data. Send it back by calling `SendAsync`. Note the use of the `EndOfMessage` flag on the receive result. This
                    // means that if this echo server is sent one continuous stream of binary data (with EndOfMessage always false) it will just stream back the same thing.
                    // If binary messages are received then the same binary messages are sent back.
                    else
                    {
                        var received = new ArraySegment<byte>(receiveBuffer, 0, receiveResult.Count);
                        var stream = new MemoryStream(receiveBuffer, 0, receiveResult.Count);
                        var reader = new BinaryReader(stream);

                        int evt = reader.ReadByte();
                        if (evt == 1)
                        {
                            PointerEventType pointerEvent = (PointerEventType)reader.ReadByte();
                            switch (pointerEvent)
                            {
                                case PointerEventType.Enter:
                                    break;
                                case PointerEventType.Moved:
                                    float x = reader.ReadInt16();
                                    float y = reader.ReadInt16();
                                    Input.PointerEvent(new Vector2(x, y), Input.PointerEventType.Moved);
                                    break;
                                case PointerEventType.Down:
                                    Blitter.PointerEvents.Add(PointerEventType.Down);
                                    break;
                                case PointerEventType.Up:
                                    Blitter.PointerEvents.Add(PointerEventType.Up);
                                    break;
                                case PointerEventType.Left:
                                    break;
                                case PointerEventType.Scrolled:
                                    float delta = reader.ReadInt16();
                                    Input.PointerEvent(new Vector2(0, delta), Input.PointerEventType.Scrolled);
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                        }
                        else if (evt == 2)
                        {
                            TextEventType textEvent = (TextEventType)reader.ReadByte();
                            switch (textEvent)
                            {
                                case TextEventType.None:
                                    break;
                                case TextEventType.Entered:
                                    int count = reader.ReadByte();
                                    var bytes = new byte[count];
                                    reader.Read(bytes, 0, count);
                                    var str = Encoding.UTF8.GetString(bytes);
                                    Blitter.TextEntered(str);
                                    break;
                                case TextEventType.Deleted:
                                    Blitter.TextEvents.Add(textEvent);
                                    break;
                                case TextEventType.Backspaced:
                                    Blitter.TextEvents.Add(textEvent);
                                    break;
                                case TextEventType.EnterPressed:
                                    Blitter.TextEvents.Add(textEvent);
                                    break;
                                case TextEventType.Right:
                                    Blitter.TextEvents.Add(textEvent);
                                    break;
                                case TextEventType.Left:
                                    Blitter.TextEvents.Add(textEvent);
                                    break;
                                case TextEventType.Copy:
                                    Blitter.TextEvents.Add(textEvent);
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                        }

                        // float X = reader.ReadSingle();
                        // float Y = reader.ReadSingle();
                        //
                        // Input.PointerEvent(new OnityEngine.Vector2(X, Y), Input.PointerEventType.Moved);

                        //await webSocket.SendAsync(received,
                        //    WebSocketMessageType.Binary, receiveResult.EndOfMessage, CancellationToken.None);

                        Blitter.OnDrawn(() =>
                        {

                        });
                    }

                    // The echo operation is complete. The loop will resume and `ReceiveAsync` is called again to wait for the next data frame.
                }
            }
            // catch (Exception e)
            // {
            //     // Just log any exceptions to the console. Pretty much any exception that occurs when calling `SendAsync`/`ReceiveAsync`/`CloseAsync` is unrecoverable in that it will abort the connection and leave the `WebSocket` instance in an unusable state.
            //     Console.Error.Write("Exception: {0}", e);
            // }
            finally
            {
                // Clean up by disposing the WebSocket once it is closed/aborted.
                if (webSocket != null)
                    webSocket.Dispose();
            }
        }

        private void SendToClient(Connection connection)
        {
            var webSocket = connection.WebSocketContext.WebSocket;
            var span = GetDeltaSIMD(connection._lastSend, out var send, out var size, out var countStartSame, out var countEndSame,
                out var sendBytes);

            if (send)
            {
                var _stream = new MemoryStream();
                var _writer = new BinaryWriter(_stream);
                _writer.Write((ushort)size);//928 
                //766
                _writer.Write((ushort)countStartSame);
                _writer.Write((ushort)countEndSame);
                _writer.Write(span);
                _writer.Flush();

                var toSend = _stream.ToArray();

                var task = webSocket.SendAsync(toSend,
                    WebSocketMessageType.Binary, true, CancellationToken.None);

                connection._lastSend = sendBytes;
            }
        }

        //private byte[] GetDelta2(byte[] _lastSend, out bool send, out int size, out int countStartSame, out int countEndSame,
        //    out byte[] sendBytes)
        //{
        //    sendBytes = Blitter.WrittenBytes.ToArray();
        //    var lastSend = _lastSend;

        //    if (lastSend == null)
        //    {
        //        send = true;
        //        size = sendBytes.Length;
        //        countStartSame = 0;
        //        countEndSame = 0;
        //        return sendBytes.ToArray();
        //    }


        //    int n = Math.Min(lastSend.Length, sendBytes.Length);

        //    unsafe
        //    {
        //        countStartSame = 0;
        //        countEndSame = 0;

        //        fixed (byte* ap = sendBytes)
        //        fixed (byte* bp = lastSend)
        //        fixed (byte* endAp = &sendBytes[^1])
        //        fixed (byte* endBp = &lastSend[^1])
        //        {
        //            {
        //                int len = n;
        //                long* alp = (long*)ap;
        //                long* blp = (long*)bp;

        //                //countStartSame
        //                //by int64
        //                while (len >= 8)
        //                {
        //                    if (*alp == *blp)
        //                    {
        //                        countStartSame += 8;
        //                        alp++;
        //                        blp++;
        //                        len -= 8;
        //                    }
        //                    else //by byte
        //                    {
        //                        byte* ap2 = (byte*)alp, bp2 = (byte*)blp;
        //                        while (len > 0)
        //                        {
        //                            if (*ap2 == *bp2)
        //                            {
        //                                countStartSame += 1;
        //                                ap2++;
        //                                bp2++;
        //                                len -= 1;
        //                            }
        //                            else
        //                            {
        //                                goto StopStart;
        //                            }
        //                        }

        //                        goto Result;
        //                    }
        //                }

        //                goto Result;
        //            }

        //        StopStart:
        //            {
        //                int len = n;

        //                var alp = (long*)endAp;
        //                var blp = (long*)endBp;

        //                //countEndSame
        //                //by int64
        //                while (len >= 8)
        //                {
        //                    if (*alp == *blp)
        //                    {
        //                        countEndSame += 8;
        //                        alp--;
        //                        blp--;
        //                        len -= 8;
        //                    }
        //                    else //by byte
        //                    {
        //                        var ap2 = (byte*)alp;
        //                        var bp2 = (byte*)blp;

        //                        while (len > 0)
        //                        {
        //                            if (*ap2 == *bp2)
        //                            {
        //                                countEndSame += 1;
        //                                ap2--;
        //                                bp2--;
        //                                len -= 1;
        //                            }
        //                            else
        //                            {
        //                                goto Result;
        //                            }
        //                        }

        //                        goto Result;
        //                    }
        //                }

        //                goto Result;
        //            }

        //        Result:
        //            {
        //                if (countStartSame == sendBytes.Length)
        //                {
        //                    send = false;
        //                    size = sendBytes.Length;
        //                    countStartSame = 0;
        //                    countEndSame = 0;
        //                    return sendBytes;
        //                }

        //                send = true;
        //                size = sendBytes.Length - countEndSame - countStartSame;
        //                var span = new byte[size];
        //                Array.Copy(sendBytes, countStartSame, span, 0, size);
        //                return span;
        //            }
        //        }
        //    }
        //}

        //private byte[] GetDelta(byte[] _lastSend, out bool send, out int size, out int countStartSame, out int countEndSame,
        //    out byte[] sendBytes)
        //{
        //    sendBytes = Blitter.WrittenBytes.ToArray();
        //    var lastSend = _lastSend;

        //    send = true;
        //    countStartSame = 0;
        //    countEndSame = 0;

        //    if (lastSend != null)
        //    {
        //        int n = Math.Min(lastSend.Length, sendBytes.Length);

        //        Span<long> sendLongs = MemoryMarshal.Cast<byte, long>(sendBytes);
        //        Span<long> lastLongs = MemoryMarshal.Cast<byte, long>(lastSend);

        //        Span<long> FromEnd(byte[] array)
        //        {
        //            var shift = array.Length % sizeof(long);
        //            var fromEnd = new Span<byte>(array, shift, array.Length - shift);
        //            Span<long> result = MemoryMarshal.Cast<byte, long>(fromEnd);
        //            return result;
        //        }

        //        Span<long> sendLongsFromEnd = FromEnd(sendBytes);
        //        Span<long> lastLongsFromEnd = FromEnd(lastSend);

        //        int ln = Math.Min(lastLongs.Length, sendLongs.Length);

        //        int li = 0;//by longs
        //        for (; li < ln; li++)
        //        {
        //            if (lastLongs[li] == sendLongs[li])
        //            {
        //                countStartSame += sizeof(long);
        //            }
        //            else
        //            {
        //                break;
        //            }
        //        }

        //        //by bytes
        //        for (int i = li * sizeof(long); i < n; i++)
        //        {
        //            if (lastSend[i] == sendBytes[i])
        //            {
        //                countStartSame++;
        //            }
        //            else break;
        //        }

        //        //if not same
        //        if (countStartSame < sendBytes.Length)
        //        {
        //            li = 1;//by longs aligned
        //            for (; li < ln; li++)
        //            {
        //                if (lastLongsFromEnd[^li] == sendLongsFromEnd[^li])
        //                {
        //                    countEndSame += sizeof(long);
        //                }
        //                else break;
        //            }

        //            //by bytes aligned
        //            var prevLi = li - 1;
        //            var lastCheckedI = prevLi * sizeof(long);

        //            for (int i = lastCheckedI + 1; i < n; i++)
        //            {
        //                if (lastSend[^i] == sendBytes[^i])
        //                {
        //                    countEndSame++;
        //                }
        //                else break;
        //            }


        //            // for (int i = 1; i < n; i++)
        //            // {
        //            //     if (lastSend[^i] == sendBytes[^i])
        //            //     {
        //            //         countEndSame++;
        //            //     }
        //            //     else break;
        //            // }
        //        }
        //    }

        //    if (countStartSame == sendBytes.Length)
        //    {
        //        send = false;
        //        size = 0;
        //        return null;
        //    }

        //    Debug.WriteLine($"{sendBytes.Length} {countEndSame} {countStartSame}");

        //    size = sendBytes.Length - countEndSame - countStartSame;
        //    if (size < 0) throw new InvalidOperationException();
        //    var span = new byte[size];
        //    Array.Copy(sendBytes, countStartSame, span, 0, size);
        //    return span;
        //}

        //private byte[] GetDelta3(byte[] _lastSend, out bool send, out int size, out int countStartSame, out int countEndSame, out byte[] sendBytes)
        //{
        //    sendBytes = Blitter.WrittenBytes.ToArray();
        //    var lastSend = _lastSend;

        //    send = true;
        //    countStartSame = 0;
        //    countEndSame = 0;

        //    if (lastSend != null)
        //    {
        //        int n = Math.Min(lastSend.Length, sendBytes.Length);

        //        const int lsize = sizeof(long);

        //        ref byte refSend = ref MemoryMarshal.GetArrayDataReference(sendBytes);
        //        ref byte refLast = ref MemoryMarshal.GetArrayDataReference(lastSend);

        //        var shift1 = sendBytes.Length % lsize;
        //        var shift2 = lastSend.Length % lsize;

        //        ref byte refSendEnd = ref Unsafe.Add(ref refSend, shift1);
        //        ref byte refLastEnd = ref Unsafe.Add(ref refLast, shift2);

        //        int refSendEndSize = sendBytes.Length - shift1;
        //        int refLastEndSize = lastSend.Length - shift2;

        //        int ln = Math.Min(lastSend.Length / lsize, sendBytes.Length / lsize);

        //        int li = 0;//by longs
        //        for (; li < ln; li++)
        //        {
        //            ref byte sht = ref Unsafe.Add(ref refSend, li * lsize);
        //            ref byte sht2 = ref Unsafe.Add(ref refLast, li * lsize);

        //            ref long long1 = ref Unsafe.As<byte, long>(ref sht);
        //            ref long long2 = ref Unsafe.As<byte, long>(ref sht2);

        //            if (long1 == long2)
        //            {
        //                countStartSame += sizeof(long);
        //            }
        //            else
        //            {
        //                break;
        //            }
        //        }

        //        //by bytes
        //        for (int i = li * lsize; i < n; i++)
        //        {
        //            if (lastSend[i] == sendBytes[i])
        //            {
        //                countStartSame++;
        //            }
        //            else break;
        //        }

        //        //if not same
        //        if (countStartSame < sendBytes.Length)
        //        {
        //            li = 1;//by longs aligned
        //            for (; li < ln; li++)
        //            {
        //                ref byte sht = ref Unsafe.Add(ref refSendEnd, refSendEndSize - li * lsize);
        //                ref byte sht2 = ref Unsafe.Add(ref refLastEnd, refLastEndSize - li * lsize);

        //                ref long long1 = ref Unsafe.As<byte, long>(ref sht);
        //                ref long long2 = ref Unsafe.As<byte, long>(ref sht2);

        //                if (long1 == long2)
        //                {
        //                    countEndSame += sizeof(long);
        //                }
        //                else break;
        //            }

        //            //by bytes aligned
        //            var prevLi = li - 1;
        //            var lastCheckedI = prevLi * sizeof(long);

        //            for (int i = lastCheckedI + 1; i < n; i++)
        //            {
        //                if (lastSend[^i] == sendBytes[^i])
        //                {
        //                    countEndSame++;
        //                }
        //                else break;
        //            }


        //            // for (int i = 1; i < n; i++)
        //            // {
        //            //     if (lastSend[^i] == sendBytes[^i])
        //            //     {
        //            //         countEndSame++;
        //            //     }
        //            //     else break;
        //            // }
        //        }
        //    }

        //    if (countStartSame == sendBytes.Length)
        //    {
        //        send = false;
        //        size = 0;
        //        return null;
        //    }

        //    Debug.WriteLine($"{sendBytes.Length} {countEndSame} {countStartSame}");

        //    size = sendBytes.Length - countEndSame - countStartSame;
        //    if (size < 0) throw new InvalidOperationException();
        //    var span = new byte[size];
        //    Array.Copy(sendBytes, countStartSame, span, 0, size);
        //    return span;
        //}

        private byte[] GetDeltaSIMD(byte[] _lastSend, out bool send, out int size, out int countStartSame, out int countEndSame, out byte[] sendBytes)
        {
            sendBytes = Blitter.WrittenBytes.ToArray();
            var lastSend = _lastSend;

            send = true;
            countStartSame = 0;
            countEndSame = 0;

            if (lastSend != null)
            {
                int n = Math.Min(lastSend.Length, sendBytes.Length);

                int lsize = Vector<byte>.Count;

                ref byte refSend = ref MemoryMarshal.GetArrayDataReference(sendBytes);
                ref byte refLast = ref MemoryMarshal.GetArrayDataReference(lastSend);

                var shift1 = sendBytes.Length % lsize;
                var shift2 = lastSend.Length % lsize;

                ref byte refSendEnd = ref Unsafe.Add(ref refSend, shift1);
                ref byte refLastEnd = ref Unsafe.Add(ref refLast, shift2);

                int refSendEndSize = sendBytes.Length - shift1;
                int refLastEndSize = lastSend.Length - shift2;

                int ln = n / lsize; //Math.Min(lastSend.Length / lsize, sendBytes.Length / lsize);

                int li = 0;//by longs
                for (; li < ln; li++)
                {
                    ref byte sht = ref Unsafe.Add(ref refSend, li * lsize);
                    ref byte sht2 = ref Unsafe.Add(ref refLast, li * lsize);

                    ref Vector<byte> long1 = ref Unsafe.As<byte, Vector<byte>>(ref sht);
                    ref Vector<byte> long2 = ref Unsafe.As<byte, Vector<byte>>(ref sht2);

                    if (long1 == long2)
                    {
                        countStartSame += lsize;
                    }
                    else
                    {
                        break;
                    }
                }

                //by bytes
                for (int i = li * lsize; i < n; i++)
                {
                    if (lastSend[i] == sendBytes[i])
                    {
                        countStartSame++;
                    }
                    else break;
                }

                //if not same
                if (countStartSame < n)
                {
                    int endN = n - countStartSame;
                    int endNL = endN / lsize;

                    li = 1;//by longs aligned
                    for (; li < endNL; li++)
                    {
                        ref byte sht = ref Unsafe.Add(ref refSendEnd, refSendEndSize - li * lsize);
                        ref byte sht2 = ref Unsafe.Add(ref refLastEnd, refLastEndSize - li * lsize);

                        ref Vector<byte> long1 = ref Unsafe.As<byte, Vector<byte>>(ref sht);
                        ref Vector<byte> long2 = ref Unsafe.As<byte, Vector<byte>>(ref sht2);

                        if (long1 == long2)
                        {
                            countEndSame += lsize;
                        }
                        else break;
                    }

                    //by bytes aligned
                    var prevLi = li - 1;
                    var lastCheckedI = prevLi * lsize;

                    for (int i = lastCheckedI + 1; i < endN; i++)
                    {
                        if (lastSend[^i] == sendBytes[^i])
                        {
                            countEndSame++;
                        }
                        else break;
                    }
                }
            }

            if (countStartSame == sendBytes.Length)
            {
                send = false;
                size = 0;
                return null;
            }

            Debug.WriteLine($"{sendBytes.Length} {countEndSame} {countStartSame}");

            //int lastSize = lastSend.Length - countEndSame - countStartSame;



            size = sendBytes.Length - countEndSame - countStartSame;
            if (size < 0)
            {//????
                countStartSame = 0;
                countEndSame = 0;
                size = sendBytes.Length;
            }


            //int minSize = Math.Min(lastSize, size);

            //if (minSize > Vector<byte>.Count)
            //{

            //    ref byte refSend = ref MemoryMarshal.GetArrayDataReference(sendBytes);
            //    ref byte refLast = ref MemoryMarshal.GetArrayDataReference(lastSend);

            //    for (int i = countStartSame; i < countStartSame + lastSize - Vector<byte>.Count; i++)
            //    {
            //        ref byte checkRef = ref Unsafe.Add(ref refLast, i);
            //        for (int j = countStartSame; j < countStartSame + size - Vector<byte>.Count; i++)
            //        {
            //            ref byte check2Ref = ref Unsafe.Add(ref refLast, j);
            //            if(check2Ref == checkRef)
            //            {

            //            }
            //        }
            //    }
            //}

            var span = new byte[size];
            Array.Copy(sendBytes, countStartSame, span, 0, size);
            return span;
        }

        public void TrySend()
        {
            foreach (var connection in _connections.Values)
            {
                SendToClient(connection);
            }
        }
    }




    // This extension method wraps the BeginGetContext / EndGetContext methods on HttpListener as a Task, using a helper function from the Task Parallel Library (TPL).
    // This makes it easy to use HttpListener with the C# 5 asynchrony features.
    public static class HelperExtensions
    {
        public static Task GetContextAsync(this HttpListener listener)
        {
            return Task.Factory.FromAsync<HttpListenerContext>(listener.BeginGetContext, listener.EndGetContext,
                TaskCreationOptions.None);
        }
    }
}