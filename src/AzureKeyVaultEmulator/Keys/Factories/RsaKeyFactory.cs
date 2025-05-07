using System.Security.Cryptography;

namespace AzureKeyVaultEmulator.Keys.Factories
{
    public static class RsaKeyFactory
    {
        private const int _defaultSize = 2048;

        public static RSA CreateRsaKey(int? keySize)
        {
            var adjustedSize = (keySize is not null && keySize != 0) ? keySize : _defaultSize;

            return RSA.Create(adjustedSize ?? _defaultSize);
        }
    }
}
