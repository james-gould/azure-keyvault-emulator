using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureKeyVaultEmulator.Shared.Constants
{
    // Hardcoded here because it's a mocking tool.
    // Means any carryover between sessioned containers will work.
    // Also I'm lazy.
    public sealed class RsaPem
    {
        public const string FullPem = @"-----BEGIN PRIVATE KEY-----
                MIIEvwIBADANBgkqhkiG9w0BAQEFAASCBKkwggSlAgEAAoIBAQCofdCGPszXrWF7Jxwlhh6m9h56
                9YhbscSK6I9zYkuBBgO0WbyZfZPimGNN5e+LqereEnAUa/ggDaTWldHjmStDz0O9dYr/9v8kWpxJ
                QmO6MpzBO/07yuUfCwRjdE8XzbG2BSEgLlVHyTgYDV83h9ThBN5xsiZ0bjqMKS1QrPu75uWBh+WH
                QmtDqWO7fx5Ilr/gdCGPBBRsA02/FBZbSRqKYfpFbz7aqtJhUD2QLeqzLq4fQ6JledqU6FDJ6XOP
                XEMlaoRaD3/s5odixOao0LlsTq7LWcNU1f2Au4t91lEGuzOLhlPH03sC18N+8Mg1vmOUl7589phG
                SFEyxO74sy2dAgMBAAECggEBAJ39O2ZlxJYIEXv09EOLO3q7FWGekbnJOs41uy0qYjoddaPK8TnL
                sruqwJLupGuFbKHHEClWBFep84LzANg1a4gt9QrWCPxyklN4U0uuYOzbQHlA0vcaDTXKktbe3Lsp
                ORXAQYt3ZqflWh/TihD74PUOJ7bcoYpTQbrjcYZQbcuF9JfX3Tv5spJKmLpHWKyL4XxBZizH92Zp
                OqgyuG/VwYyZRfU+9wPNs0pcHkgyFSF4b5n8b3iU6wNGmJJlfu9lOD8ev2L8aZUqdrqJOcCwK4rG
                BpS4tRXv4Hk9XTMGjl56xEyHk6juF2iMckLhYmRt6Q7/OfURxa2bLZdiByDOPlECgYEA2RCActeE
                CFQSCpNPapq6MsnrDETMP2CABMkwOpiWO2vOZs/Y5JWlIas2AlPx+CGL9WyxSAWtPRJN5FQXRgLS
                kcnUw6ZBMaGlcfa5a8Pd9ovAoLuPeCwpxFej+5HneDjWkRQsmRHKjXpWvruOHkhQdbv3T+smTsfd
                fgAIzERy69sCgYEAxrbdEYXW9FMAELNoqWo48PfLz1x0ouDRjHD+XgZCfNy1/fHXsFMZjo0UYT5D
                /Y1L7xfvYPf8DsVETsuUN8KLGBHR+wRf0zYf9Q663Hvk5XVJNnWBs0LHwgMxGNffC06HqGsA4RW1
                eeDN00JP5T3cyNmCN5iDcM4udPBzDlilgecCgYEAtVwxRkLFYUQE8usT7qkqq6bDibOtx8I0FEuY
                zUySMUGo6YP93zcdCp2HebhzsnMtAjj3goqjrSQvCngsHeXb082DxJiTXgmGN0sCr4SuXwFzR5iO
                jcSwfQkQzO+iK3Op6vulK5uO1liCQ8hnPOwEten//7kkf6xEZrNWpn0GXAMCgYEAqgyEo+En8M8y
                aBhPwWKwNa2oENxqx5OiXw+27ZlnvlhVuWoDDNYgMbgTL6BMKKeIyqNt60prvewcJ13ZidoGk+N0
                EN5Obn2L3Xbse4/ecmnq7BqkklXcge+fTUY2jgN23a4sA3JDaXfySw4dNuy4inxwDcmK+bbHVLUL
                kMRVZhMCgYA2GX8rOCkmlnJWmG3sYD0fP2KzSF5NxAFQq3F39IvwvJ+Inkx2agdZFBynjsg4VJSS
                tHfC5zD1uQ8sd/4hCbsK2yQuEsiS+j7Ij15MZvFdJCwxPKV99BTFuxIhnit5xEnqVzb7KNugwDp4
                Yu1oryGTUP3I2wF4ta4teRH/4y01Iw==
                -----END PRIVATE KEY-----";
    }
}
