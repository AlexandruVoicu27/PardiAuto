using System;

namespace AutoPartsShop
{
    public class PlataFactura
    {
        public Guid? IDPlata { get; set; }
        public Guid? IDFactura { get; set; }
        public Guid IDComanda { get; set; }
        public string NumarFactura { get; set; }
        public string Client { get; set; }
        public DateTime DataComanda { get; set; }
        public DateTime? DataPlata { get; set; }
        public decimal TotalFactura { get; set; }
        public decimal SumaPlatita { get; set; }
        public string Metoda { get; set; }
        public string StatusPlata { get; set; }
        public string StatusFactura { get; set; }

        public PlataFactura(Guid? idPlata, Guid? idFactura, Guid idComanda, string numarFactura, string client, DateTime dataComanda, DateTime? dataPlata, decimal totalFactura, decimal sumaPlatita, string metoda, string statusPlata, string statusFactura)
        {
            IDPlata = idPlata;
            IDFactura = idFactura;
            IDComanda = idComanda;
            NumarFactura = numarFactura;
            Client = client;
            DataComanda = dataComanda;
            DataPlata = dataPlata;
            TotalFactura = totalFactura;
            SumaPlatita = sumaPlatita;
            Metoda = metoda;
            StatusPlata = statusPlata;
            StatusFactura = statusFactura;
        }
    }
}
