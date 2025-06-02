using System.ComponentModel.DataAnnotations;

namespace digitalArsv1.DTOs
{
    public class CrearCuentaDTO
    {
        // todo se genera en backend.

    }


    public class CuentaConsultaDTO
    {
        public int NroCuenta { get; set; }
        public int NroCliente { get; set; }
        public string Producto { get; set; } = default!;    // "Caja de ahorro"
        public string? CBU { get; set; } = default!;    // Prefijo fijo (268006110208+ aleatorio (10)_total22
        public bool Estado { get; set; }            // true de activa
        public string RolCta { get; set; } = default!;    // "Titular"
        public decimal Saldo { get; set; }                // inicial 0
        public DateTime FechaCreacion { get; set; }
        public string Alias { get; set; } = string.Empty;


        // DTO para mostrar un resumen de las cuentas de un cliente:
        /// solo NroCuenta, CBU, Nombre y Apellido del titular, y Saldo.
        /// </summary>
        public class CuentaResumenDTO
        {

            /// Número de cuenta (últimos 5 dígitos del CBU).
            public int NroCuenta { get; set; }

            /// CBU completo de 22 dígitos.
            public string CBU { get; set; } = default!;
            /// Nombre del titular (desde la entidad Usuario).
            public string Nombre { get; set; } = default!;

            /// Apellido del titular (desde la entidad Usuario).
            public string Apellido { get; set; } = default!;

            /// Saldo actual de la cuenta.
            public decimal Saldo { get; set; }
            public DateTime FechaCreacion { get; set; }
            public string Alias { get; set; }
        }

        /// DTO para retornar el saldo final de una cuenta tras procesar el lote de movimientos.
        public class CuentaSaldoDTO
        {
            
        /// Número de cuenta (nro_cuenta).
          public int NroCuenta { get; set; }

            
         /// Saldo resultante tras aplicar todos los movimientos.
           
            public decimal Saldo { get; set; }
        }
        /// DTO para actualizar alias de la cuenta
        public class UpdateAliasDTO
        {
            public int NroCuenta { get; set; }
            public string NuevoAlias { get; set; } = default!;
        }



    }
}















