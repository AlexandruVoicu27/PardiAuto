using System;

namespace AutoPartsShop
{
    public class Produs
    {
        public Guid ID { get; set; }
        public string Nume { get; set; } 
        public string Descriere { get; set; } 
        public string Categorie { get; set; } 
        public decimal Pret { get; set; }
        public int Cantitate { get; set; }

        public Produs(Guid id, string nume, string descriere, string categorie, decimal pret, int cantitate)
        {
            ID = id;
            Nume = nume;
            Descriere = descriere;
            Categorie = categorie;
            Pret = pret;
            Cantitate = cantitate;
        }
    }
}
