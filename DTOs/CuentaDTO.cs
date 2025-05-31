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
        public bool Estado { get; set; }                // true de activa
        public string RolCta { get; set; } = default!;    // "Titular"
        public decimal Saldo { get; set; }                // inicial 0
        public DateTime FechaCreacion { get; set; }
    }

    // ✅ NUEVO: incluir lista de cuentas del usuario
    // DTO para mostrar un resumen de las cuentas de un cliente:
    /// solo NroCuenta, CBU, Nombre y Apellido del titular, y Saldo.
    /// </summary>
    public class CuentaResumenDTO
    {
        /// <summary>
        /// Número de cuenta (últimos 5 dígitos del CBU).
        /// </summary>
        public int NroCuenta { get; set; }

        /// <summary>
        /// CBU completo de 22 dígitos.
        /// </summary>
        public string CBU { get; set; } = default!;

        /// <summary>
        /// Nombre del titular (desde la entidad Usuario).
        /// </summary>
        public string Nombre { get; set; } = default!;

        /// <summary>
        /// Apellido del titular (desde la entidad Usuario).
        /// </summary>
        public string Apellido { get; set; } = default!;

        /// <summary>
        /// Saldo actual de la cuenta.
        /// </summary>
        public decimal Saldo { get; set; }
    }
   
    
        /// <summary>
        /// DTO para retornar el saldo final de una cuenta tras procesar el lote de movimientos.
        /// </summary>
        public class CuentaSaldoDTO
        {
            /// <summary>
            /// Número de cuenta (nro_cuenta).
            /// </summary>
            public int NroCuenta { get; set; }

            /// <summary>
            /// Saldo resultante tras aplicar todos los movimientos.
            /// </summary>
            public decimal Saldo { get; set; }
        }
    }















