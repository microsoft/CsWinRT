// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace System.Threading.Tasks;

/// <summary>
/// Reusable component to generate unique IDs for any of the different implementations of <see cref="global::Windows.Foundation.IAsyncInfo"/>.
/// </summary>
internal static class AsyncInfoIdGenerator
{
    /// <summary>
    /// We will never generate this ID, so this value can be used as an invalid, uninitialised or a <em>no-ID</em> value.
    /// </summary>
    public const uint InvalidId = int.MaxValue;

    /// <summary>
    /// The <see cref="Random"/> instance to generate the IDs (not thread-safe).
    /// </summary>
    /// <remarks>
    /// We want to avoid ending up with the same ID as a Windows-implemented async info.
    /// At the same time we want to be reproducible. So we use a random generator with a fixed seed.
    /// </remarks>
    private static readonly Random IdGenerator = new(19830118);

    /// <summary>
    /// Generate a unique ID that can be used for an <see cref="global::Windows.Foundation.IAsyncInfo"/> object.
    /// </summary>
    /// <returns>A new unique <see cref="global::Windows.Foundation.IAsyncInfo"/> ID.</returns>
    public static uint CreateNext()
    {
        lock (IdGenerator)
        {
            // Valid IDs will be larger than zero and smaller than 'InvalidId'
            int id = IdGenerator.Next(1, (int)InvalidId);

            return unchecked((uint)id);
        }
    }

    /// <summary>
    /// Initialises the specified <paramref name="id"/> value to a unique ID that can be used for an
    /// <see cref="global::Windows.Foundation.IAsyncInfo"/> object object under the assumption that another
    /// thread may also attempt to initialise <paramref name="id"/>. The thread that changes <paramref name="id"/>
    /// first from <see cref="InvalidId"/> to another value wins and all other threads will respect that
    /// choice and leave <paramref name="id"/> unchanged. The method returns the ID that was agreed upon by the race.
    /// </summary>
    /// <param name="id">The <see cref="global::Windows.Foundation.IAsyncInfo"/> ID to initialise.</param>
    /// <returns>The unique value to which the specified reference target was initialised.</returns>
    public static uint EnsureInitialized(ref uint id)
    {
        uint originalId = Volatile.Read(ref id);

        // If the original value we observed is not 'InvalidId', it means some other
        // thread won the race and initialized the value. So we can just return it.
        if (originalId != InvalidId)
        {
            return originalId;
        }

        uint newId = CreateNext();

        // Try to replace the target location with our local ID
        uint previousId = Interlocked.CompareExchange(
            location1: ref id,
            value: newId,
            comparand: InvalidId);

        // If we won the race, return our new ID, otherwise return the previous
        // value, which will be the valid ID set by the thread winning the race.
        return previousId == InvalidId ? newId : previousId;
    }
}