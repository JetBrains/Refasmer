using System;
using System.Security.Cryptography;

namespace JetBrains.Refasmer
{
    public static class PublicKeyTokenCalculator
    {
        public static byte[] CalculatePublicKeyToken( byte[] publicKey )
        {
            using var sha1Algo = SHA1.Create();
            var hash = sha1Algo.ComputeHash(publicKey);
            
            var publicKeyToken = new byte [8];
            Array.Copy(hash, hash.Length-publicKeyToken.Length, publicKeyToken, 0, publicKeyToken.Length);
            Array.Reverse(publicKeyToken, 0, publicKeyToken.Length);
            
            return publicKeyToken;
        }
        
    }
}