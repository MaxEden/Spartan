using StbImageSharp;
using System.Numerics;
using System.Runtime.InteropServices;
using Silk.NET.OpenGLES;

namespace Spartan.Silk
{
    public class TexQuadList
    {
        private TexVertex[] _array = new TexVertex[1000];
        private int[] _indices = new int[1000];
        private int _vertCount;
        private int _indCount;
        private Rect? _clipRect;
        //private Vector2 _texSize;
        private Vector2 _invSize;
        private Rect _solidRect;
        private Vector2 _posShift;

        private Quad[] _quads = new Quad[50];
        private int _quadCount;

        public bool Clip(Rect clipRect, ref Rect rect, ref Rect uvRect)
        {
            if (rect.xMax < clipRect.xMin) return false;
            if (rect.xMin > clipRect.xMax) return false;
            if (rect.yMax < clipRect.yMin) return false;
            if (rect.yMin > clipRect.yMax) return false;

            if (clipRect.Contains(rect))
            {
                return true;
            }

            var newRect = Rect.MinMaxRect(
                MathF.Max(clipRect.xMin, rect.xMin),
                MathF.Max(clipRect.yMin, rect.yMin),
                MathF.Min(clipRect.xMax, rect.xMax),
                MathF.Min(clipRect.yMax, rect.yMax)
            );

            static float InvLerp(float value, float a, float b)
            {
                return (value - a) / (b - a);
            }

            static float Lerp(float a, float b, float t)
            {
                return a + (b - a) * t;
            }

            var newUvRect = Rect.MinMaxRect(
                Lerp(uvRect.xMin, uvRect.xMax, InvLerp(newRect.xMin, rect.xMin, rect.xMax)),
                Lerp(uvRect.yMin, uvRect.yMax, InvLerp(newRect.yMin, rect.yMin, rect.yMax)),
                Lerp(uvRect.xMin, uvRect.xMax, InvLerp(newRect.xMax, rect.xMin, rect.xMax)),
                Lerp(uvRect.yMin, uvRect.yMax, InvLerp(newRect.yMax, rect.yMin, rect.yMax))
            );

            rect = newRect;
            uvRect = newUvRect;

            return true;
        }

        public void Init(Vector2 texSize, Rect solidRect)
        {
            //_texSize = texSize;
            _invSize = new Vector2(1f / texSize.X, 1f / texSize.Y);
            _solidRect = solidRect;
        }

        public void AddSolid(Rect rect, Color32 color)
        {
            Add(_solidRect, rect, color);
        }

        public void Add(Rect from, Rect rect, Color32 color)
        {
            rect.X += _posShift.X;
            rect.Y += _posShift.Y;

            if (_clipRect.HasValue)
            {
                if(!Clip(_clipRect.Value, ref rect, ref from)) return;
            }
            
            if (_vertCount + 4 >= _array.Length)
            {
                Array.Resize(ref _array, _array.Length * 2 + 4);
            }

            int vertShift = _vertCount;
            
            _array[vertShift + 0] = new TexVertex()
            {
                Color = color.ToFloat(),
                Position = new Vector2(rect.X, rect.Y),
                Uv = new Vector2(from.X, from.Y) * _invSize
            };
            _array[vertShift + 1] = new TexVertex()
            {
                Color = color.ToFloat(),
                Position = new Vector2(rect.X, rect.Y + rect.Height),
                Uv = new Vector2(from.X, from.Y + from.Height) * _invSize
            };
            _array[vertShift + 2] = new TexVertex()
            {
                Color = color.ToFloat(),
                Position = new Vector2(rect.X + rect.Width, rect.Y + rect.Height),
                Uv = new Vector2(from.X + from.Width, from.Y + from.Height) * _invSize
            };
            _array[vertShift + 3] = new TexVertex()
            {
                Color = color.ToFloat(),
                Position = new Vector2(rect.X + rect.Width, rect.Y),
                Uv = new Vector2(from.X + from.Width, from.Y) * _invSize
            };
            _vertCount += 4;

            if (_indCount + 6 >= _indices.Length)
            {
                Array.Resize(ref _indices, _indices.Length * 2 + 5);
            }

            int indexShift = _indCount;

            _indices[indexShift + 0] = vertShift;
            _indices[indexShift + 1] = vertShift + 1;
            _indices[indexShift + 2] = vertShift + 2;

            _indices[indexShift + 3] = vertShift;
            _indices[indexShift + 4] = vertShift + 2;
            _indices[indexShift + 5] = vertShift + 3;

            _indCount += 6;
        }

        public void AddGraphic(STexture texture,  Rect rect, Color32 color)
        {
            AddGraphic(texture, new Rect(Vector2.Zero, texture.Size), rect, color);
        }

        public void AddGraphic(STexture texture , Rect from, Rect rect, Color32 color)
        {
            if(texture.TexID == 0) return;

            rect.X += _posShift.X;
            rect.Y += _posShift.Y;

            if (_clipRect.HasValue)
            {
                if (!Clip(_clipRect.Value, ref rect, ref from)) return;
            }

            if (_quadCount + 1 >= _quads.Length)
            {
                Array.Resize(ref _quads, _quads.Length * 2 + 4);
            }

            var quad = new Quad();

            quad.TexID = texture.TexID;

            var invSize = texture.InvSize;

            quad.TexVertex1 = new TexVertex()
            {
                Color = color.ToFloat(),
                Position = new Vector2(rect.X, rect.Y),
                Uv = new Vector2(from.X, from.Y) * invSize
            };
            quad.TexVertex2 = new TexVertex()
            {
                Color = color.ToFloat(),
                Position = new Vector2(rect.X, rect.Y + rect.Height),
                Uv = new Vector2(from.X, from.Y + from.Height) * invSize
            };
            quad.TexVertex3 = new TexVertex()
            {
                Color = color.ToFloat(),
                Position = new Vector2(rect.X + rect.Width, rect.Y + rect.Height),
                Uv = new Vector2(from.X + from.Width, from.Y + from.Height) * invSize
            };
            quad.TexVertex4 = new TexVertex()
            {
                Color = color.ToFloat(),
                Position = new Vector2(rect.X + rect.Width, rect.Y),
                Uv = new Vector2(from.X + from.Width, from.Y) * invSize
            };
            _quads[_quadCount] = quad;
            _quadCount += 1;
        }

        public void Clear()
        {
            _vertCount = 0;
            _indCount = 0;
            _quadCount = 0;
        }

        public ReadOnlySpan<Quad> GetQuadsSpan()
        {
            return new ReadOnlySpan<Quad>(_quads, 0, _quadCount);
        }

        public ReadOnlySpan<TexVertex> GetVertexSpan()
        {
            return new ReadOnlySpan<TexVertex>(_array, 0, _vertCount);
        }

        public ReadOnlySpan<int> GetIndexSpan()
        {
            return new ReadOnlySpan<int>(_indices, 0, _indCount);
        }

        public void Clip(Rect rect)
        {
            _clipRect = rect;
        }

        public void EndClip()
        {
            _clipRect = null;
        }

        public void BeginScroll(float shift, Rect area, float height)
        {
            _posShift = new Vector2(area.X, area.Y - shift);
            Clip(area);
        }

        public void EndScroll()
        {
            _posShift = default;
            EndClip();
        }

        public static int[] QuadIndexes = new int[6]
        {
            0,
            1,
            2,
            0,
            2,
            3
        };
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct TexVertex
    {
        public Vector2 Position;
        public ColorF Color;
        public Vector2 Uv;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Quad
    {
        public uint TexID;
        public TexVertex TexVertex1;
        public TexVertex TexVertex2;
        public TexVertex TexVertex3;
        public TexVertex TexVertex4;
    }

    public class STexture
    {
        public uint TexID;
        public Vector2 Size;
        public Vector2 InvSize;
        public ImageResult LoadResult;
        public Task Task;
        public byte[] Bytes;
        public string Path;

        public unsafe void FinishLoad(GL gl)
        {
            var font = LoadResult;
            var fontTex = gl.GenTexture();
            gl.BindTexture(TextureTarget.Texture2D, fontTex);
            fixed (byte* ptr = font.Data)
            {
                gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba,
                    (uint)font.Width, (uint)font.Height,
                    0, PixelFormat.Rgba, PixelType.UnsignedByte, ptr);
            }

            gl.TexParameterI(GLEnum.Texture2D, GLEnum.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            gl.TexParameterI(GLEnum.Texture2D, GLEnum.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            gl.TexParameterI(GLEnum.Texture2D, GLEnum.TextureMinFilter, (int)TextureMinFilter.Nearest);
            gl.TexParameterI(GLEnum.Texture2D, GLEnum.TextureMagFilter, (int)TextureMagFilter.Nearest);

            var fontSize = new Vector2(font.Width, font.Height);

            gl.BindTexture(TextureTarget.Texture2D, 0);

            TexID = fontTex;
            Size = fontSize;
            InvSize = new Vector2(1f / fontSize.X, 1f / fontSize.Y);
        }
    }
}
