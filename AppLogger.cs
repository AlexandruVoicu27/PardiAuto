using System;
using System.IO;

namespace AutoPartsShop
{
    // Scrie actiunile importante ale utilizatorilor intr-un fisier local de audit.
    public static class AppLogger
    {
        private static readonly string FisierAudit = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AuditAplicatie.txt");

        // Adauga in fisier data, actiunea si detaliile primite, fara sa opreasca aplicatia daca logarea esueaza.
        public static void Scrie(string actiune, string detalii)
        {
            try
            {
                string linie = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss") + " | " + actiune + " | " + detalii;
                File.AppendAllText(FisierAudit, linie + Environment.NewLine);
            }
            catch
            {
                //loggerul nu trebuie sa opreasca aplicatia
            }
        }
    }
}
