using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AutoPartsShop
{
    
    public static class Security
    {
        
        // Transforma parola intr-un hash SHA-256 reprezentat hexadecimal.
        public static string HashParola(string parola)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(parola));
                StringBuilder builder = new StringBuilder();

                // Fiecare byte devine doua caractere hexazecimale.
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}
