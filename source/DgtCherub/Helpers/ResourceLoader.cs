using System.IO;
using System.Reflection;

namespace DgtCherub.Helpers
{
    public class ResourceLoader
    {
        public static string LoadResourceString(string resourceName)
        {
            using Stream resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
            using StreamReader reader = new(resourceStream);

            return reader.ReadToEnd();
        }

        public static byte[] LoadResource(string resourceName)
        {
            using Stream resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
            using MemoryStream memoryStream = new();
            resourceStream.CopyTo(memoryStream);

            return memoryStream.ToArray();
        }
    }
}
