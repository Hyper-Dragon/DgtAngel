using System.Reflection;

namespace DgtCherub.Helpers
{
    internal sealed class ResourceLoader
    {
        internal static string LoadResourceString(in string resourceName)
        {
            using Stream resourceStream = OpenResource(resourceName);
            using StreamReader reader = new(resourceStream);

            return reader.ReadToEnd();
        }

        internal static byte[] LoadResource(in string resourceName)
        {
            using Stream resourceStream = OpenResource(resourceName);
            using MemoryStream memoryStream = new();
            resourceStream.CopyTo(memoryStream);

            return memoryStream.ToArray();
        }

        /// <summary>
        /// Loads an embedded resource, or reports that it is not there. For a name that came
        /// from a request - a clock's asset - absence is an answer, not a fault, and the
        /// caller replies 404. The strict forms above are for names the build must supply.
        /// </summary>
        internal static bool TryLoadResource(in string resourceName, out byte[] content)
        {
            content = Array.Empty<byte>();

            using Stream resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);

            if (resourceStream is null)
            {
                return false;
            }

            using MemoryStream memoryStream = new();
            resourceStream.CopyTo(memoryStream);
            content = memoryStream.ToArray();

            return true;
        }

        /// <summary>
        /// GetManifestResourceStream answers null for a name that is not embedded, so callers
        /// used to dereference null and throw a NullReferenceException that named nothing.
        /// A resource the build was meant to carry is a defect in the build: name it, and say
        /// what is actually available.
        /// </summary>
        private static Stream OpenResource(string resourceName)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            Stream resourceStream = assembly.GetManifestResourceStream(resourceName);

            if (resourceStream is null)
            {
                string available = string.Join(", ", assembly.GetManifestResourceNames());
                throw new InvalidOperationException(
                    $"Embedded resource '{resourceName}' was not found. Available resources: {available}");
            }

            return resourceStream;
        }
    }
}
