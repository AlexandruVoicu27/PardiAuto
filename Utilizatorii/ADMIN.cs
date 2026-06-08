using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoPartsShop
{
    public class ADMIN:Utilizator
    {
        public ADMIN(int id,string nume, string email) : base(id,nume, email, RolUtilizator.Administrator)
        {
        }
    }
}
