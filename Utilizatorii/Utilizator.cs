using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoPartsShop
{
    /// <summary>
    /// Modelul de baza pentru un utilizator autentificat.
    /// </summary>
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

        //este angajat? creeaza un obiect de tip ANGAJAT etc
        public static Utilizator CreateByRol(int id, string nume, string email, RolUtilizator rol)
        {
            return rol switch
            {
                RolUtilizator.Administrator => new ADMIN(id, nume, email),
                RolUtilizator.Angajat => new ANGAJAT(id, nume, email),
                RolUtilizator.Client => new CLIENT(id, nume, email),
            };
        }
    }
}
