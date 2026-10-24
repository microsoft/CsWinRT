// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;
using WindowsRuntime.InteropServices.Marshalling;

namespace UnitTest;

[TestClass]
public unsafe class ArrayMarshallingLifetimeTests
{
    private const int BufferLength = 4;

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(3)]
    public void ManagedValueArray_InjectedConversionFailureDisposesResourcesExactlyOnce(int failureIndex)
    {
        ResourceMarshaller.DisposalCounts = new int[BufferLength + 1];
        ResourceMarshaller.ConversionsBeforeFailure = failureIndex;
        Resource[] values = Enumerable.Range(1, BufferLength).Select(i => new Resource(i)).ToArray();
        int[] destination = new int[BufferLength];
        fixed (int* data = destination)
        {
            int* buffer = data;
            try
            {
                Assert.ThrowsExactly<InvalidOperationException>(() =>
                    WindowsRuntimeManagedValueTypeArrayMarshaller<Resource, int>.CopyToUnmanaged<ResourceMarshaller>(
                        values, BufferLength, buffer));
                CollectionAssert.AreEqual(new int[BufferLength + 1], ResourceMarshaller.DisposalCounts,
                    "Copying must leave ownership with its caller.");
            }
            finally
            {
                WindowsRuntimeManagedValueTypeArrayMarshaller<Resource, int>.Dispose<ResourceMarshaller>(BufferLength, buffer);
            }
            int[] expectedDisposals = Enumerable.Range(0, BufferLength + 1)
                .Select(value => value > 0 && value <= failureIndex ? 1 : 0).ToArray();
            CollectionAssert.AreEqual(expectedDisposals, ResourceMarshaller.DisposalCounts,
                "Caller cleanup must dispose each acquired resource once and leave unacquired resources untouched.");
        }
    }

    private readonly record struct Resource(int Value);

    private readonly struct ResourceMarshaller : IWindowsRuntimeManagedValueTypeArrayElementMarshaller<Resource, int>
    {
        [ThreadStatic]
        public static int[] DisposalCounts;

        [ThreadStatic]
        public static int ConversionsBeforeFailure;

        public static int ConvertToUnmanaged(Resource value)
        {
            if (ConversionsBeforeFailure-- == 0)
            {
                throw new InvalidOperationException("Injected element conversion failure.");
            }
            return value.Value;
        }

        public static Resource ConvertToManaged(int value) => new(value);

        public static void Dispose(int value)
        {
            if (value != 0)
            {
                DisposalCounts[value]++;
            }
        }
    }
}
