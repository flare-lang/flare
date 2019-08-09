using System;

namespace Flare.Runtime
{
    [Serializable]
    public sealed class ModuleLoadException : Exception
    {
        public ModuleLoadException(string message, Exception? inner = null)
            : base(message, inner)
        {
        }
    }
}
