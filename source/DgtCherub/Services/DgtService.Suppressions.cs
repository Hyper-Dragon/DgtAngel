using System.Diagnostics.CodeAnalysis;

namespace DgtCherub.Services
{
    /// <summary>
    /// Analysis suppressions for <see cref="DgtService"/>, kept apart from the service itself.
    /// </summary>
    /// <remarks>
    /// DgtService mirrors the proto definition member for member, so it is the file most likely to be
    /// regenerated or re-scaffolded when the contract changes. Suppressions live here so that a
    /// regeneration cannot quietly drop them and refill the build with the same warnings.
    /// </remarks>
    [SuppressMessage(
        "Design",
        "CA1062:Validate arguments of public methods",
        Justification = "Every member this applies to is an override of a gRPC service base method. " +
                        "gRPC deserialises the request message from the wire before the method is called, " +
                        "so the argument is never null and a guard in each override would be unreachable code.")]
    public partial class DgtService
    {
    }
}
