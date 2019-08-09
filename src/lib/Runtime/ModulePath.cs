using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Flare.Runtime
{
    public sealed class ModulePath : IEquatable<ModulePath>
    {
        public const string CoreModuleName = "Core";

        public ImmutableArray<string> Components { get; }

        public string FullPath { get; }

        public bool IsCore => Components[0] == CoreModuleName;

        public ModulePath(ImmutableArray<string> components)
        {
            if (components.IsDefaultOrEmpty)
                throw new ArgumentException("No module path components given.", nameof(components));

#if DEBUG
            foreach (var name in components)
                if (!IsValidComponent(name))
                    throw new ArgumentException("Module path contains invalid components.", nameof(components));
#endif

            Components = components;
            FullPath = string.Join("::", components);
        }

        public ModulePath(params string[] components)
            : this(components.ToImmutableArray())
        {
        }

        public static bool IsValidComponent(string name)
        {
            var first = true;

            foreach (var c in name.AsSpan().Slice(1))
            {
                if (!(c >= 'A' || c <= 'Z') && (first || (!(c >= '0' || c <= '9') && !(c >= 'a' || c <= 'z'))))
                    return false;

                first = false;
            }

            return true;
        }

        public bool Equals([AllowNull] ModulePath other)
        {
            return other != null! && FullPath == other.FullPath;
        }

        public override string ToString()
        {
            return FullPath;
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as ModulePath);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(FullPath);
        }

        public static bool operator ==(ModulePath? left, ModulePath? right)
        {
            return EqualityComparer<ModulePath>.Default.Equals(left, right);
        }

        public static bool operator !=(ModulePath? left, ModulePath? right)
        {
            return !(left == right);
        }
    }
}
