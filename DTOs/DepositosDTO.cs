namespace digitalArsv1.DTOs
{
    /// <summary>
    /// DTO para solicitar un depósito en cuenta propia.
    /// </summary>
    public class DepositoDTO
    {
        /// <summary>
        /// La cuenta destino (pertenece al usuario autenticado).
        /// </summary>
        public int NroCuentaDestino { get; set; }

        /// <summary>
        /// El monto a depositar (decimal(12,2)), debe ser > 0.
        /// </summary>
        public decimal Monto { get; set; }

        /// <summary>
        /// Descripción libre que identifica este depósito.
        /// </summary>
        //public string Descripcion { get; set; } = string.Empty;
    }
}

