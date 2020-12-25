using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Flare
{
    public static class DebugAssert
    {
        [Conditional("DEBUG")]
        [SuppressMessage("Microsoft.Usage", "CA2201", Justification = "Intentional.")]
        public static void Check(bool condition, string message)
        {
            if (!condition)
                throw new Exception($"Assertion failed: {message}");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SuppressMessage("Microsoft.Usage", "CA2201", Justification = "Intentional.")]
        public static Exception Unreachable()
        {
            return new Exception("Unreachable code executed.");
        }
    }
}
