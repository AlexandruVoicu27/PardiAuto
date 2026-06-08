using System;

namespace AutoPartsShop
{
    // Model folosit de tabelul din pagina Administrare comenzi.
    // Grupeaza datele comenzii, clientului si facturii intr-un singur obiect afisabil.
    public class AdminComanda
    {
        public Guid ID { get; set; }
        public string Client { get; set; }
        public string Email { get; set; }
        public DateTime DataComanda { get; set; }
        public string Status { get; set; }
        public decimal Total { get; set; }
        public int NumarProduse { get; set; }
        public string Observatii { get; set; }
        public bool AreFactura { get; set; }

        // Construieste modelul unei comenzi citite din baza de date.
        public AdminComanda(Guid id, string client, string email, DateTime dataComanda, string status, decimal total, int numarProduse, string observatii, bool areFactura)
        {
            ID = id;
            Client = client;
            Email = email;
            DataComanda = dataComanda;
            Status = status;
            Total = total;
            NumarProduse = numarProduse;
            Observatii = observatii;
            AreFactura = areFactura;
        }
    }
}
