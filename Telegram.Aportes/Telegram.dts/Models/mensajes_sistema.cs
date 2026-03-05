using System;
using System.Collections.Generic;

namespace Telegram.dts.Models;

public partial class mensajes_sistema
{
    public int id { get; set; }

    public string? clave { get; set; }

    public string? texto { get; set; }

    public string idioma { get; set; } = null!;

    public sbyte? baja { get; set; }
}
