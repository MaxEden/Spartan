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
        
        public static Color32 clear => new Color32(0, 0, 0, 0);
        public static Color32 white => new Color32(255, 255, 255, 255);
        public static Color32 black => new Color32(0, 0, 0, 255);
        public static Color32 blue => new Color32(0, 0, 255, 255);

        public static Color32 sky => new Color32(0, 128, 255, 255);
        public static Color32 cyan => new Color32(0, 255, 255, 255);
        public static Color32 yellow => new Color32(255, 255, 0, 255);
        public static Color32 green => new Color32(0, 255, 0, 255);
        public static Color32 gray => new Color32(128, 128, 128, 255);
        public static Color32 red => new Color32(255, 0, 0, 255);
        public static Color32 orange => new Color32(255, 128, 0, 255);

        public Color32 Lighter()
        {
            return new Color32(Clamp(r + 25), Clamp(g + 25), Clamp(b + 25), a);
        }

        public Color32 Lighter(int add)
        {
            return new Color32(Clamp(r + add), Clamp(g + add), Clamp(b + add), a);
        }

        public byte Clamp(int value)
        {
            if (value < 0) value = 0;
            if (value > byte.MaxValue) value = byte.MaxValue;
            return (byte)value;
        }

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

        public static implicit operator Color32(Vector4 vector)
        {
            return Float(vector.X, vector.Y, vector.Z, vector.W);
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

        public float yMid => Y + Height / 2;

        public Vector2 size => new Vector2(Width, Height);

        public Rect(float x, float y, float width, float height)
        {
            this.X = x;
            this.Y = y;
            this.Width = width;
            this.Height = height;
        }

        public static Rect FromCenterSize(Vector2 center, float size)
        {
            return new Rect(center.X - size / 2, center.Y - size / 2, size, size);
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

        public bool Contains(in Vector2 point)
        {
            return point.X >= X && point.X < X + Width &&
                   point.Y >= Y && point.Y < Y + Height;
        }

        public bool Contains(in Rect rect)
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

        public bool Clip(in Rect clipRect, out Rect result)
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

        public Rect Pad(float x, float y)
        {
            return new Rect(X + x, Y + x, Width - 2 * y, Height - 2 * y);
        }
        public Rect Pad(float pad)
        {
            return new Rect(X + pad, Y + pad, Width - 2 * pad, Height - 2 * pad);
        }

        public void Split2(out Rect a, out Rect b)
        {
            a = new Rect(X + 0 * Width / 2, Y, Width / 2, Height);
            b = new Rect(X + 1 * Width / 2, Y, Width / 2, Height);
        }

        public void Split3(out Rect a, out Rect b, out Rect c)
        {
            a = new Rect(X + 0 * Width / 3, Y, Width / 3, Height);
            b = new Rect(X + 1 * Width / 3, Y, Width / 3, Height);
            c = new Rect(X + 2 * Width / 3, Y, Width / 3, Height);
        }

        public void Split4(out Rect a, out Rect b, out Rect c, out Rect d)
        {
            a = new Rect(X + 0 * Width / 4, Y, Width / 4, Height);
            b = new Rect(X + 1 * Width / 4, Y, Width / 4, Height);
            c = new Rect(X + 2 * Width / 4, Y, Width / 4, Height);
            d = new Rect(X + 3 * Width / 4, Y, Width / 4, Height);
        }

        public Rect WithZeroPos()
        {
            return new Rect(0, 0, Width, Height);
        }

        public Rect SplitTop(float height, out Rect rest)
        {
            rest = new Rect(X, Y + height, Width, Height - height);
            return new Rect(X, Y, Width, height);
        }

        public Rect SplitBottom(float height, out Rect rest)
        {
            rest = new Rect(X, Y, Width, Height - height);
            return new Rect(X, Y + Height - height, Width, height);
        }

        public Rect SplitLeft(float width, out Rect rest)
        {
            rest = new Rect(X + width, Y, Width - width, Height);
            return new Rect(X, Y, width, Height);
        }

        public Rect SplitRight(float width, out Rect rest)
        {
            rest = new Rect(X, Y, Width - width, Height);
            return new Rect(X + Width - width, Y, width, Height);
        }

        public Rect ExpandAtBottom(float height)
        {
            var rest = new Rect(X, Y + Height, Width, height);
            return rest;
        }

        public Rect MiddleStripe(float height)
        {
            return new Rect(X, yMid - height / 2, Width, height);
        }
    }
}