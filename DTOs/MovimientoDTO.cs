namespace digitalArsv1.DTOs
{
    /// <summary>
    /// DTO para representar cualquier tipo de movimiento:
    ///   - 1 = Crédito por transferencia (SUMA)
    ///   - 2 = Transferencia a otra cuenta (RESTA)
    ///   - 3 = Depósito cuenta propia (SUMA)
    ///   - 4 = Compra en comercio (RESTA)
    ///   - 5 = Recarga de saldo virtual (RESTA)
    ///
    /// Reglas de validación en el controlador según CódigoTransaccion:
    ///   - Si CodigoTransaccion == 1: NroCuentaOrigen != 0, NroCuentaDestino != null
    ///   - Si CodigoTransaccion == 2: NroCuentaOrigen != 0, NroCuentaDestino != null
    ///   - Si CodigoTransaccion == 3: NroCuentaOrigen == 0,  NroCuentaDestino != null
    ///   - Si CodigoTransaccion == 4: NroCuentaOrigen != 0, NroCuentaDestino == null
    ///   - Si CodigoTransaccion == 5: NroCuentaOrigen == 0,  NroCuentaDestino == null
    /// </summary>
    public class MovimientoDTO
    {
        /// <summary>
        /// Código de transacción:
        ///   1 = Crédito por transferencia (SUMA)
        ///   2 = Transferencia a otra cuenta (RESTA)
        ///   3 = Depósito cuenta propia (SUMA)
        ///   4 = Compra en comercio (RESTA)
        ///   5 = Recarga de saldo virtual (RESTA)
        /// </summary>
        
        public int NroCuentaOrigen { get; set; }

        /// <summary>
        /// Número de cuenta destino:
        ///   - Para códigos 1, 2 y 3 debe ser != null
        ///   - Para códigos 4 y 5 debe ser null
        /// </summary>
        public int? NroCuentaDestino { get; set; }

        /// <summary>
        /// Monto del movimiento (decimal(12,2)). Debe ser > 0.
        /// </summary>
        public decimal Monto { get; set; }

        /// <summary>
        /// Descripción opcional: texto que el usuario ingresa para este movimiento.
        /// </summary>
        public string? Descripcion { get; set; }
    }
}
