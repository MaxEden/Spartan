using System.Numerics;
using System.Runtime.InteropServices;

namespace Spartan
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Color32
    {
        public byte r;
        public byte g;
        public byte b;
        public byte a;

        public Color32(byte r, byte g, byte b, byte a)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }

        public static Color32 white => new Color32(255, 255, 255, 255);
        public static Color32 black => new Color32(0, 0, 0, 255);
        public static Color32 blue => new Color32(0, 0, 255, 255);
        public static Color32 yellow => new Color32(255, 255, 0, 255);
        public static Color32 green => new Color32(0, 255, 0, 255);
        public static Color32 gray => new Color32(128, 128, 128, 255);

        public static Color32 red => new Color32(255, 0, 0, 255);

        public static Color32 Float(float r, float g, float b, float a)
        {
            return new Color32(
                (byte)(byte.MaxValue * r),
                (byte)(byte.MaxValue * g),
                (byte)(byte.MaxValue * b),
                (byte)(byte.MaxValue * a));
        }

        public ColorF ToFloat()
        {
            return new ColorF(
                r / (float)byte.MaxValue,
                g / (float)byte.MaxValue,
                b / (float)byte.MaxValue,
                a / (float)byte.MaxValue);
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ColorF
    {
        public float r;
        public float g;
        public float b;
        public float a;

        public ColorF(float r, float g, float b, float a)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }

        public static implicit operator Color32(ColorF colorF)
        {
            return new Color32(
                (byte)(byte.MaxValue * colorF.r),
                (byte)(byte.MaxValue * colorF.g),
                (byte)(byte.MaxValue * colorF.b),
                (byte)(byte.MaxValue * colorF.a));
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Rect
    {
        public float X;
        public float Y;
        public float Width;
        public float Height;

        public Vector2 position => new Vector2(X, Y);

        public float xMin => X;
        public float yMin => Y;
        public float xMax => X + Width;
        public float yMax => Y + Height;
        public Vector2 size => new Vector2(Width, Height);

        public Rect(float x, float y, float width, float height)
        {
            this.X = x;
            this.Y = y;
            this.Width = width;
            this.Height = height;
        }

        public Rect(Vector2 position, Vector2 size)
        {
            X = position.X;
            Y = position.Y;
            Width = size.X;
            Height = size.Y;
        }

        public Rect(Rect source)
        {
            X = source.X;
            Y = source.Y;
            Width = source.Width;
            Height = source.Height;
        }

        public bool Contains(Vector2 point)
        {
            return point.X >= X && point.X < X + Width &&
                   point.Y >= Y && point.Y < Y + Height;
        }

        public bool Contains(Rect rect)
        {
            return X <= rect.X && X + Width >= rect.X + rect.Width &&
                   Y <= rect.Y && Y + Height >= rect.Y + rect.Height;
        }

        public override string ToString()
        {
            return $"{X}, {Y}, {Width}, {Height}";
        }

        public static Rect MinMaxRect(float xmin, float ymin, float xmax, float ymax)
        {
            return new Rect()
            {
                X = xmin,
                Y = ymin,
                Width = xmax - xmin,
                Height = ymax - ymin
            };
        }


        public bool Clip(Rect clipRect, out Rect result)
        {
            result = default;
            if (xMax < clipRect.xMin) return false;
            if (xMin > clipRect.xMax) return false;
            if (yMax < clipRect.yMin) return false;
            if (yMin > clipRect.yMax) return false;

            result = MinMaxRect(
                MathF.Min(clipRect.xMin, xMin),
                MathF.Min(clipRect.yMin, yMin),
                MathF.Max(clipRect.xMax, xMax),
                MathF.Max(clipRect.yMax, yMax)
            );
            return true;
        }
    }
}
