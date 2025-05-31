using digitalArsv1.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace digitalArsv1.Repositories
{
    public interface ICuentaRepository : IRepository<Cuenta>
    {
        // Métodos específicos para Cuenta 
        Task<IEnumerable<Cuenta>> GetAllWithUsuarioAsync();
        Task<Cuenta> GetByIdWithUsuarioAsync(int id);
        Task<decimal> ObtenerSaldoAsync(int nroCuenta);
        Task<bool> ExisteCuenta(int nroCuenta);
        Task CrearAsync(Cuenta cuenta);
        Task<List<Cuenta>> GetByClienteAsync(int nro_cliente);
    }
}
