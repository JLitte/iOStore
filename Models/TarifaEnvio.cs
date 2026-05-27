using iOStore.Helpers;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace iOStore.Models
{
    public class TarifaEnvio
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Transportista { get; set; } = string.Empty;   // "OCA", "Andreani", etc.

        [Required]
        [StringLength(10)]
        public string ZonaDesde { get; set; } = string.Empty;       // CP base, ej: "1000"

        [Required]
        [StringLength(10)]
        public string ZonaHasta { get; set; } = string.Empty;       // CP destino, ej: "1999"

        [Column(TypeName = "decimal(18,2)")]
        public decimal Costo { get; set; }

        public int DiasEstimados { get; set; }

        public bool Activo { get; set; } = true;

        public DateTime FechaActualizacion { get; set; } = ArClock.Now;
    }
}
