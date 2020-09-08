using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace Flare.Tests.TestAdapter
{
    static class FlareTestEnvironment
    {
        public static ImmutableHashSet<string> Symbols { get; }

        static FlareTestEnvironment()
        {
            var os = "unix";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                os = "windows";
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
                os = "freebsd";
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
                RuntimeInformation.IsOSPlatform(OSPlatform.Android))
                os = "linux";
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.macOS) ||
                RuntimeInformation.IsOSPlatform(OSPlatform.iOS) ||
                RuntimeInformation.IsOSPlatform(OSPlatform.tvOS) ||
                RuntimeInformation.IsOSPlatform(OSPlatform.watchOS))
                os = "darwin";

            var (arch, bits) = RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X86 => ("x86", "32"),
                Architecture.X64 => ("x86", "64"),
                Architecture.Arm => ("arm", "32"),
                Architecture.Arm64 => ("arm", "64"),
                _ => throw DebugAssert.Unreachable(),
            };

            Symbols = ImmutableHashSet.Create(os, $"{os}-{arch}", $"{os}-{bits}", $"{os}-{arch}-{bits}", arch,
                $"{arch}-{bits}", bits);
        }
    }
}
