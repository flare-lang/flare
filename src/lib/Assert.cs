using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Flare
{
    public static class Assert
    {
        [Conditional("DEBUG")]
        public static void Check(bool condition, string message)
        {
            if (!condition)
                throw new Exception($"Assertion failed: {message}");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Exception Unreachable()
        {
            return new Exception("Unreachable code executed.");
        }
    }
}
