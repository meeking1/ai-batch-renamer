using System;

namespace AiBatchRenamer.Tests
{
    internal static class TestAssert
    {
        public static void True(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        public static void Equal<T>(T expected, T actual, string message)
        {
            if (!object.Equals(expected, actual))
            {
                throw new InvalidOperationException(
                    string.Format("{0}. Expected: {1}. Actual: {2}.", message, expected, actual));
            }
        }
    }
}
