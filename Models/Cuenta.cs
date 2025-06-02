using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace digitalArsv1.Models
{
    public class Cuenta
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)] // ✅ Evita que SQL genere este campo automáticamente
        public int nro_cuenta { get; set; }
        public string producto { get; set; } = string.Empty;
        public string CBU { get; set; } = string.Empty;
        public bool estado { get; set; }
        public int nro_cliente { get; set; }
        public string? rol_cta { get; set; }
        public decimal? saldo { get; set; }
        public DateTime fecha_alta { get; set; }
        public string alias { get; set; } = string.Empty;

        // Relación con Usuario (nro_cliente)
        [ForeignKey("nro_cliente")]
        public Usuario? Usuario { get; set; }

        // Relación 1:N con Movimiento (origen y destino)
        public ICollection<Movimiento> MovimientosOrigen { get; set; } = new List<Movimiento>();
        public ICollection<Movimiento> MovimientosDestino { get; set; } = new List<Movimiento>();

    
    }
}