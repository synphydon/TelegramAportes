using System;
using System.Collections.Generic;

namespace Telegram.dts.Models;

public partial class usuario
{
    public int id { get; set; }

    public long? telegram_id { get; set; }

    public string? nombre_usuario { get; set; }

    public sbyte soy_mayor_10 { get; set; }

    public sbyte vendedora_contenido { get; set; }

    public sbyte consentimiento { get; set; }

    public sbyte sin_rostro { get; set; }

    public sbyte solo_mayor_edad { get; set; }

    public sbyte vendedora_verificada { get; set; }

    public DateTime fecha_ultimo_aporte { get; set; }

    public sbyte ingresar_texto { get; set; }

    public sbyte ingresar_multimedia { get; set; }

    public sbyte Ingresar_audio { get; set; }
}
