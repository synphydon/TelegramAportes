using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.dts.Models;

namespace Telegram.Aportes
{
    public static class General
    {
        public static List<dts.Models.mensajes_sistema> mensajes = new List<dts.Models.mensajes_sistema>();

        public static void LeeMensajes()
        {
            ApplicationDbContext db = new ApplicationDbContext();
            mensajes = db.mensajes_sistemas.ToList();
        }

        public static string Mensaje(string clave)
        {
            string mensaje = mensajes.FirstOrDefault(m => m.clave == clave).texto + "";
            return mensaje;
        }

    }
}
