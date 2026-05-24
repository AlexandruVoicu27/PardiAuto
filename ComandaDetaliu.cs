using System;

namespace AutoPartsShop
{
    public class ComandaDetaliu
    {
        public Guid IDComanda { get; set; }
        public DateTime DataComanda { get; set; }
        public string Produs { get; set; }
        public string Categorie { get; set; }
        public int Cantitate { get; set; }
        public decimal Pret { get; set; }
        public decimal Total { get; set; }

        public ComandaDetaliu(Guid idComanda, DateTime dataComanda, string produs, string categorie, int cantitate, decimal pret)
        {
            IDComanda = idComanda;
            DataComanda = dataComanda;
            Produs = produs;
            Categorie = categorie;
            Cantitate = cantitate;
            Pret = pret;
            Total = pret * cantitate;
        }
    }
}
