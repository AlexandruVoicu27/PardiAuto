using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoPartsShop
{
    public class Utilizator
    {
        public int ID { get; private set; }
        public string Nume { get; private set; }
        public string Email { get; private set; }
        public RolUtilizator Rol { get; private set; }

        public Utilizator(int id,string nume, string email, RolUtilizator rol)
        {
            ID = id;
            Nume = nume;
            Email = email;
            Rol = rol;
        }
    }
}
