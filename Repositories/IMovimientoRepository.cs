using digitalArsv1.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using static MovimientoDetalleDTO;

namespace digitalArsv1.Repositories
{
    public interface IMovimientoRepository : IRepository<Movimiento>
    {
        // Métodos específicos para Movimiento que no están en IRepository
        Task<IEnumerable<Movimiento>> GetAllWithRelationsAsync();

        // ✅  Agregar un movimiento al contexto (no guarda aún)
        Task CrearAsync(Movimiento movimiento);

        // ✅ Guardar cambios pendientes en el contexto
        Task SaveAsync();

        // Obtener una cuenta por su número
        Task<Cuenta> ObtenerCuentaPorNumeroAsync(int numeroCuenta);

        //  Obtener el último ID de movimiento registrado
        Task<int> GetMaxIdAsync();

        //    Agregar un nuevo movimiento
        Task AddAsync(Movimiento movimiento);

        // para obtener detalle por ID
        Task<MovimientoDetalleDTO?> GetMovimientoDetallePorIdAsync(int id);
        Task<List<MovimientoDetalleDTO>> GetResumenMovimientosPorClienteAsync(
            int nroCliente, DateTime fechaDesde, DateTime fechaHasta);
        Task<List<UltimosMovimientoDTO>> GetUltimosMovimientosPorClienteAsync(
           int nroCliente, DateTime fechaDesde, DateTime fechaHasta);
    }
}
 




    