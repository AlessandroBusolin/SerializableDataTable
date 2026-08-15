namespace Test.Shared
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Minimal, dependency-free assertion helpers used by the shared Touchstone scenarios.
    /// Each failed assertion throws so the Touchstone executor records the case as failed.
    /// </summary>
    internal static class TestAssert
    {
        internal static void True(bool condition, string message = null)
        {
            if (!condition) throw new InvalidOperationException(message ?? "Expected condition to be true.");
        }

        internal static void False(bool condition, string message = null)
        {
            if (condition) throw new InvalidOperationException(message ?? "Expected condition to be false.");
        }

        internal static void Equal<T>(T expected, T actual, string message = null)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new InvalidOperationException(message ?? ("Expected '" + Describe(expected) + "', got '" + Describe(actual) + "'."));
            }
        }

        internal static void NotEqual<T>(T expected, T actual, string message = null)
        {
            if (EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new InvalidOperationException(message ?? ("Did not expect '" + Describe(actual) + "'."));
            }
        }

        internal static void Null(object value, string message = null)
        {
            if (value != null) throw new InvalidOperationException(message ?? ("Expected value to be null but was '" + Describe(value) + "'."));
        }

        internal static void NotNull(object value, string message = null)
        {
            if (value == null) throw new InvalidOperationException(message ?? "Expected value to be non-null.");
        }

        internal static void Contains(string haystack, string needle, string message = null)
        {
            if (haystack == null || !haystack.Contains(needle))
            {
                throw new InvalidOperationException(message ?? ("Expected text to contain '" + needle + "'."));
            }
        }

        internal static void DoesNotContain(string haystack, string needle, string message = null)
        {
            if (haystack != null && haystack.Contains(needle))
            {
                throw new InvalidOperationException(message ?? ("Expected text not to contain '" + needle + "'."));
            }
        }

        internal static void SequenceEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual, string message = null)
        {
            if (expected == null || actual == null || !expected.SequenceEqual(actual))
            {
                throw new InvalidOperationException(message ?? "Expected sequences to be equal.");
            }
        }

        internal static void FloatArrayClose(float[] expected, float[] actual, float tolerance = 0.0001f, string message = null)
        {
            NotNull(actual, message ?? "Expected non-null float array.");
            if (expected.Length != actual.Length)
                throw new InvalidOperationException(message ?? ("Expected length " + expected.Length + " but got " + actual.Length + "."));

            for (int i = 0; i < expected.Length; i++)
            {
                float diff = Math.Abs(expected[i] - actual[i]);
                float allowed = Math.Abs(expected[i]) * tolerance;
                if (expected[i] == 0f) allowed = tolerance;
                if (diff > allowed)
                    throw new InvalidOperationException(message ?? ("Value mismatch at index " + i + ": expected " + expected[i] + ", got " + actual[i] + "."));
            }
        }

        internal static TException Throws<TException>(Action action, string message = null) where TException : Exception
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            try
            {
                action();
            }
            catch (TException exception)
            {
                return exception;
            }
            catch (Exception other)
            {
                throw new InvalidOperationException((message ?? "Wrong exception type.") + " Expected " + typeof(TException).Name + " but received " + other.GetType().Name + ".", other);
            }

            throw new InvalidOperationException(message ?? ("Expected exception of type " + typeof(TException).Name + " but none was thrown."));
        }

        internal static async Task<TException> ThrowsAsync<TException>(Func<Task> action, string message = null) where TException : Exception
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            try
            {
                await action().ConfigureAwait(false);
            }
            catch (TException exception)
            {
                return exception;
            }

            throw new InvalidOperationException(message ?? ("Expected exception of type " + typeof(TException).Name + " but none was thrown."));
        }

        private static string Describe(object value)
        {
            if (value == null) return "null";
            if (value is IEnumerable enumerable && !(value is string))
            {
                List<string> parts = new List<string>();
                foreach (object item in enumerable) parts.Add(item == null ? "null" : item.ToString());
                return "[" + string.Join(",", parts) + "]";
            }
            return value.ToString();
        }
    }
}
