using System;
using System.Security.Cryptography;

namespace JetBrains.Refasmer
{
    public static class PublicKeyTokenCalculator
    {
        public static byte[] CalculatePublicKeyToken( byte[] publicKey )
        {
            var sha1Algo = new SHA1Managed();
            var hash = sha1Algo.ComputeHash(publicKey);
            
            var publicKeyToken = new byte [8];
            Array.Copy(hash, hash.Length-publicKeyToken.Length, publicKeyToken, 0, publicKeyToken.Length);
            Array.Reverse(publicKeyToken, 0, publicKeyToken.Length);
            
            return publicKeyToken;
        }
        
    }
}