using System;

namespace AutoPartsShop
{
    public class AdminComanda
    {
        public Guid ID { get; set; }
        public int IDUtilizator { get; set; }
        public string Client { get; set; }
        public string Email { get; set; }
        public DateTime DataComanda { get; set; }
        public string Status { get; set; }
        public decimal Total { get; set; }
        public int NumarProduse { get; set; }
        public string Observatii { get; set; }
        public bool AreFactura { get; set; }

        public AdminComanda(Guid id, int idUtilizator, string client, string email, DateTime dataComanda, string status, decimal total, int numarProduse, string observatii, bool areFactura)
        {
            ID = id;
            IDUtilizator = idUtilizator;
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
