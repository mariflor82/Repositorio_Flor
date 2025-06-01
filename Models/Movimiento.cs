using System;                                   // ✅ AGREGADO: para DateTime
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace digitalArsv1.Models
{
    public class Movimiento
    {
        [Key]
        public int id_trx { get; set; }
        public DateTime fecha { get; set; }
        public decimal monto { get; set; }

        // Foreign Keys

       public int? nro_cuenta_orig { get; set; }    //  permite null para depósitos sin origen
       public int? nro_cuenta_dest { get; set; }    //  permite null si no hay destino

        public int codigo_transaccion { get; set; }

        [ForeignKey("nro_cuenta_orig")]
        public Cuenta? CuentaOrig { get; set; }      //  ahora nullable en correspondencia con el FK

        [ForeignKey("nro_cuenta_dest")]
        public Cuenta? CuentaDest { get; set; }

        [ForeignKey("codigo_transaccion")]
        public Transaccion? Transaccion { get; set; }
    }
}