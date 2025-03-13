using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using SFML.Graphics;
using SFML.System;
using Spartan;

namespace Spartan.SFML
{
    internal static class Converter
    {
        public static IntRect ToIntRect(this Rect rect)
        {
            return new IntRect((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height);
        }

        public static Vector2f ToVector2f(this Vector2 vect)
        {
            return new Vector2f(vect.X, vect.Y);
        }
    }
}
