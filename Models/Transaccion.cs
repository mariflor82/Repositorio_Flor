using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace digitalArsv1.Models
{
    public class Transaccion
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int codigo_transaccion { get; set; }

        public string descripcion { get; set; } = string.Empty;

        // Relación 1:N con Movimientos
        public ICollection<Movimiento> Movimientos { get; set; } = new List<Movimiento>();
    }
}
