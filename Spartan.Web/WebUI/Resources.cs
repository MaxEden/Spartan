using System.Reflection;

namespace Spartan.Web
{
    internal class Resources
    {
        public static Dictionary<string, byte[]> Loaded = new();

        public static void LoadAll()
        {
            Loaded.Clear();
            var asm = Assembly.GetExecutingAssembly();
            var resourceNames = asm.GetManifestResourceNames();
            foreach (var resourceName in resourceNames)
            {
                var stream = asm.GetManifestResourceStream(resourceName);
                using var memoryStream = new MemoryStream();
                stream.CopyTo(memoryStream);
                var bytes = memoryStream.ToArray();
                var key = resourceName.Replace("Spartan.Web.Resources.", "");
                Loaded[key] = bytes;
            }
        }
    }
}
