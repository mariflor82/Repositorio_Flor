using digitalArsv1.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace digitalArsv1.Repositories
{
    public interface IUsuarioRepository : IRepository<Usuario>
    {
        Task<List<Usuario>> ObtenerUsuariosSinCuentaAsync();
        Task DesactivarUsuariosAsync(List<Usuario> usuarios);
        Task<Usuario> ObtenerPorEmailAsync(string email);
        Task<Usuario?> GetByMailWithCuentasAsync(string mail); // ← para ver cuentas de usuario
        Task CrearAsync(Usuario usuario);
        Task SaveAsync();
    }
}
