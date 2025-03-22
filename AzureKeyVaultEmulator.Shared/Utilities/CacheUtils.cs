using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureKeyVaultEmulator.Shared.Utilities
{
    public static class CacheUtils
    {
        public static string GetCacheId(this string name, string version = "")
        {
            return $"{name}{version}";
        }
    }
}
