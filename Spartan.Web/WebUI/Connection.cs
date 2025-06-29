using System.Net.WebSockets;
using System.Numerics;
using static Spartan.Input;
using System.Text;
using TestProgram;

namespace Spartan.Web.WebUI;

class Connection
{
    public WebSocketContext WebSocketContext;
    public int Id;
    public byte[] ReceiveBuffer = new byte[1024 * 4];

    private byte[] _lastSend;
    private TestProgram1 _program;
    private WebBlitter _blitter;
    private Input Input => _program.input;

    public void Start()
    {
        _blitter = new WebBlitter();
        _program = new TestProgram1();
        _program.blitter = _blitter;
        _blitter.Input = _program.input;
        _program.Create();
    }

    public void Update()
    {
        _program.input.Layout.ViewSize = new Vector2(1000, 500);
        _program.Update();
        SendToClient(this);
    }

    public void ReceiveBinary(WebSocketReceiveResult receiveResult)
    {
        var stream = new MemoryStream(ReceiveBuffer, 0, receiveResult.Count);
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
                    Input.PointerEvent(new Vector2(x, y), PointerEventType.Moved);
                    break;
                case PointerEventType.Down:
                    _blitter.PointerEvents.Add(PointerEventType.Down);
                    break;
                case PointerEventType.Up:
                    _blitter.PointerEvents.Add(PointerEventType.Up);
                    break;
                case PointerEventType.Left:
                    break;
                case PointerEventType.Scrolled:
                    float delta = reader.ReadInt16();
                    Input.PointerEvent(new Vector2(0, -delta), PointerEventType.Scrolled);
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
                case TextEventType.Typed:
                    int count = reader.ReadByte();
                    var bytes = new byte[count];
                    var read = reader.Read(bytes, 0, count);
                    var str = Encoding.UTF8.GetString(bytes);
                    _blitter.TextEntered(str);
                    break;
                case TextEventType.Deleted:
                    _blitter.TextEvents.Add(textEvent);
                    break;
                case TextEventType.Backspaced:
                    _blitter.TextEvents.Add(textEvent);
                    break;
                case TextEventType.EnterPressed:
                    _blitter.TextEvents.Add(textEvent);
                    break;
                case TextEventType.Right:
                    _blitter.TextEvents.Add(textEvent);
                    break;
                case TextEventType.Left:
                    _blitter.TextEvents.Add(textEvent);
                    break;
                case TextEventType.Copy:
                    _blitter.TextEvents.Add(textEvent);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        Update();
    }

        private void SendToClient(Connection connection)
    {
        var webSocket = connection.WebSocketContext.WebSocket;
        var span = Delta.GetDeltaSimd(_blitter, connection._lastSend, out var send, out var size, out var countStartSame, out var countEndSame,
            out var sendBytes);

        if (send)
        {
            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream);
            writer.Write((ushort)size);//928 
            //766
            writer.Write((ushort)countStartSame);
            writer.Write((ushort)countEndSame);
            writer.Write(span);
            writer.Flush();

            var toSend = stream.ToArray();

            var task = webSocket.SendAsync(toSend, WebSocketMessageType.Binary, true, CancellationToken.None);

            connection._lastSend = sendBytes;
        }
    }
}