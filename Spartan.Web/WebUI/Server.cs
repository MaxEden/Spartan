using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;

namespace Spartan.Web.WebUI
{
    internal class Server
    {
        private HttpListener _httpListener;
        public void Start()
        {
            Resources.LoadAll();
            _httpListener = new HttpListener();
            _httpListener.Prefixes.Add("http://localhost:4444/");
            _httpListener.Start();
            Console.WriteLine("Service started at: "+ _httpListener.Prefixes.First());
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
                    ProcessWebSocketRequest(context);
                }
                else if (request.HttpMethod == "GET")
                {
                    ProcessGet(request, context);
                }

                Receive();
            }
        }

        private static void ProcessGet(HttpListenerRequest request, HttpListenerContext context)
        {
            if (request.RawUrl == "/fontBlack")
            {
                var response = context.Response;
                response.StatusCode = (int)HttpStatusCode.OK;
                response.ContentType = "image/png";
                response.OutputStream.Write(Resources.Loaded["fontBlack.png"]);
                response.OutputStream.Close();
            }

            if (request.RawUrl == "/fontWhite")
            {
                var response = context.Response;
                response.StatusCode = (int)HttpStatusCode.OK;
                response.ContentType = "image/png";
                response.OutputStream.Write(Resources.Loaded["font.png"]);
                response.OutputStream.Close();
            }
            else
            {
                var response = context.Response;
                response.StatusCode = (int)HttpStatusCode.OK;
                response.ContentType = "text/html";
                response.OutputStream.Write(Resources.Loaded["Page.html"]);
                response.OutputStream.Close();
            }
        }

        private int _uk = 0;
        private readonly ConcurrentDictionary<int, Connection> _connections = new();

        private async void ProcessWebSocketRequest(HttpListenerContext listenerContext)
        {
            Connection connection = null;
            try
            {
                //### Accepting WebSocket connections
                // Calling `AcceptWebSocketAsync` on the `HttpListenerContext` will accept the WebSocket connection, sending the required 101 response to the client
                // and return an instance of `WebSocketContext`. This class captures relevant information available at the time of the request and is a read-only 
                // type - you cannot perform any actual IO operations such as sending or receiving using the `WebSocketContext`. These operations can be 
                // performed by accessing the `System.Net.WebSocket` instance via the `WebSocketContext.WebSocket` property.
                // 
                // When calling `AcceptWebSocketAsync` the negotiated subprotocol must be specified. This sample assumes that no subprotocol 
                // was requested. 
                WebSocketContext webSocketContext = await listenerContext.AcceptWebSocketAsync(subProtocol: null);
                Interlocked.Increment(ref _uk);
                connection = new Connection()
                {
                    Id = _uk,
                    WebSocketContext = webSocketContext
                };
                connection.Start();
                _connections.TryAdd(_uk, connection);
                Console.WriteLine("Connection added: {0}", connection.Id);

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
                    WebSocketReceiveResult receiveResult = await webSocket.ReceiveAsync(connection.ReceiveBuffer, CancellationToken.None);

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
                    else if (receiveResult.MessageType == WebSocketMessageType.Text)
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.InvalidMessageType, "Cannot accept text frame", CancellationToken.None);
                    }
                    // Otherwise we must have received binary data. Send it back by calling `SendAsync`. Note the use of the `EndOfMessage` flag on the receive result. This
                    // means that if this echo server is sent one continuous stream of binary data (with EndOfMessage always false) it will just stream back the same thing.
                    // If binary messages are received then the same binary messages are sent back.
                    else
                    {
                        connection.ReceiveBinary(receiveResult);
                    }

                    //The loop will resume and `ReceiveAsync` is called again to wait for the next data frame.
                }
            }
            catch (Exception e)
            {
                // Just log any exceptions to the console. Pretty much any exception that occurs when calling `SendAsync`/`ReceiveAsync`/`CloseAsync` is unrecoverable in that it will abort the connection and leave the `WebSocket` instance in an unusable state.
                Console.Error.Write("Exception: {0}", e);
            }
            finally
            {
                // Clean up by disposing the WebSocket once it is closed/aborted.
                webSocket?.Dispose();

                _connections.Remove(connection.Id, out _);
                Console.WriteLine("Connection removed: {0}", connection.Id);
            }
        }
    }

}