namespace digitalArsv1.DTOs
{
    /// <summary>
    /// DTO para solicitar un depósito en cuenta propia.
    /// </summary>
    public class DepositoDTO
    {
     /// El monto a depositar (decimal(12,2)), debe ser > 0.
      public decimal Monto { get; set; }

       
    }
}

