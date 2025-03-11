using System.IO;

namespace TestBlit.WebUI
{
    public static class Write
    {
        public static void WriteUShortComp(BinaryWriter writer, int value)
        {
            if (value <= 127)
            {
                writer.Write((byte)value);
            }
            else
            {

                var a = (byte)(value >> 8 | 128);
                var b = (byte)(value & 255);

                writer.Write(a);
                writer.Write(b);
            }
        }

        internal static void WriteRect(BinaryWriter _writer, Rect rectDest)
        {
            _writer.Write((short)rectDest.X);
            _writer.Write((short)rectDest.Y);
            _writer.Write((short)rectDest.Width);
            _writer.Write((short)rectDest.Height);
        }

        internal static void WriteColor(BinaryWriter _writer, Color32 color)
        {
            _writer.Write(color.r);
            _writer.Write(color.g);
            _writer.Write(color.b);
            _writer.Write(color.a);
        }
    }
}
