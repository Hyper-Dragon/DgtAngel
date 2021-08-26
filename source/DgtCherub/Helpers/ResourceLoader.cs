using System.IO;
using System.Reflection;

namespace DgtCherub.Helpers
{
    internal sealed class ResourceLoader
    {
        internal static string LoadResourceString(in string resourceName)
        {
            using Stream resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
            using StreamReader reader = new(resourceStream);

            return reader.ReadToEnd();
        }

        internal static byte[] LoadResource(in string resourceName)
        {
            using Stream resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
            using MemoryStream memoryStream = new();
            resourceStream.CopyTo(memoryStream);

            return memoryStream.ToArray();
        }
    }
}
