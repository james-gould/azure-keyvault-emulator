using System.Security.Cryptography;

namespace AzureKeyVaultEmulator.Shared.Persistence.Utils;

public static class RsaParametersSerializer
{
    public static byte[] Serialize(RSAParameters p)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        Write(p.D);
        Write(p.DP);
        Write(p.DQ);
        Write(p.Exponent);
        Write(p.InverseQ);
        Write(p.Modulus);
        Write(p.P);
        Write(p.Q);

        return ms.ToArray();

        void Write(byte[]? data)
        {
            if (data == null)
            {
                writer.Write(-1);
            }
            else
            {
                writer.Write(data.Length);
                writer.Write(data);
            }
        }
    }

    public static RSAParameters Deserialize(byte[] data)
    {
        using var ms = new MemoryStream(data);
        using var reader = new BinaryReader(ms);

        return new RSAParameters
        {
            D = Read(),
            DP = Read(),
            DQ = Read(),
            Exponent = Read(),
            InverseQ = Read(),
            Modulus = Read(),
            P = Read(),
            Q = Read()
        };

        byte[]? Read()
        {
            int len = reader.ReadInt32();
            return len == -1 ? null : reader.ReadBytes(len);
        }
    }
}
