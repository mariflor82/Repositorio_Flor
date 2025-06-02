namespace digitalArsv1.DTOs

{ 

        //ver movimientos de la cuenta
        //filtro fechas
    public class RangoFechasMovDTO
        {
            public DateTime FechaDesde { get; set; }
            public DateTime FechaHasta { get; set; }
        }
    }

// ==== DTO para Detalle de Movimiento ====
public class MovimientoDetalleDTO
{
    public int IdTrx { get; set; }
    public string DescripcionTransaccion { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public DateTime Fecha { get; set; }
    public int? NroCuentaOrigen { get; set; }
    public int? NroCuentaDestino { get; set; }

    // ==== DTO para Ultimos movimientos ====
    public class UltimosMovimientoDTO
    {
        public DateTime Fecha { get; set; }
        public string DescripcionTransaccion { get; set; } = string.Empty;
        public decimal Monto { get; set; }
        

    }
}