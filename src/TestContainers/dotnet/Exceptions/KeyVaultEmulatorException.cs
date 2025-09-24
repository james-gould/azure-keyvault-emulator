using System;

namespace AzureKeyVaultEmulator.TestContainers.Exceptions
{
    internal class KeyVaultEmulatorException : Exception
    {
        public KeyVaultEmulatorException(string msg) : base(msg)
        {
        }
    }
}
