using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoPartsShop
{
    public class CLIENT:Utilizator
    {
        public CLIENT(int id,string nume, string email) : base(id,nume, email, RolUtilizator.Client)
        {
        }
    }
}
