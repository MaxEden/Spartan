using System.Numerics;

namespace Spartan.TestApi
{
    internal class Class1
    {
        public string FontName = "Arial Froot";
        public int Height  = 1;
        public Vector2 Size = new Vector2(2.3f, 4.5f);
        public Vector3 Size3 = new Vector3(6.78f, 9.10f,0);
        public bool Debug = false;

        public int TestCallerTimes;

        public EnumVariants Enum;

        private Random random = new();
        public void GenerateNewSize3()
        {
            Size3.X = random.NextSingle();
            Size3.Y = random.NextSingle();
            Size3.Z = random.NextSingle();
        }

        public void TestCall()
        {
            TestCallerTimes++;
            DebugConsole.Log("Test Called "+ TestCallerTimes);
        }
    }

    public enum EnumVariants
    {
        None,
        A,
        B,
        C
    }
}