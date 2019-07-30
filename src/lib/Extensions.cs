using System;
using System.Runtime.CompilerServices;

namespace Flare
{
    static class Extensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Check<T>(this T value, string name)
            where T : Enum
        {
#if DEBUG
            var t = typeof(T);

            if (!Enum.IsDefined(t, value))
                throw new ArgumentException($"Invalid {t.Name} value.", name);
#endif

            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T CheckFlags<T>(this T value, string name)
            where T : Enum
        {
#if DEBUG
            var t = typeof(T);
            var ch = value.ToString()[0];

            if (ch == '-' || char.IsDigit(ch))
                throw new ArgumentException($"Invalid {t.Name} flags combination.", name);
#endif

            return value;
        }
    }
}
