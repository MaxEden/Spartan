using System.Diagnostics;
using System.Net.WebSockets;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
                    Input.PointerEvent(new Vector2(0, delta), PointerEventType.Scrolled);
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

    private byte[] GetDeltaSimd(byte[] lastSend, out bool send, out int size, out int countStartSame, out int countEndSame, out byte[] sendBytes)
    {
        sendBytes = _blitter.WrittenBytes.ToArray();

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
    private void SendToClient(Connection connection)
    {
        var webSocket = connection.WebSocketContext.WebSocket;
        var span = GetDeltaSimd(connection._lastSend, out var send, out var size, out var countStartSame, out var countEndSame,
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