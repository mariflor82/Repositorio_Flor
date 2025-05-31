using digitalArsv1.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace digitalArsv1.Repositories
{
    public class UsuarioRepository : Repository<Usuario>, IUsuarioRepository
    {
        public UsuarioRepository(DigitalArsContext context) : base(context)
        {
        }

        public async Task<List<Usuario>> ObtenerUsuariosSinCuentaAsync()
        {
            return await _context.Usuarios
                .Where(u => !_context.Cuentas.Any(c => c.nro_cliente == u.nro_cliente))
                .ToListAsync();
        }

        public async Task DesactivarUsuariosAsync(List<Usuario> usuarios)
        {
            foreach (var usuario in usuarios)
            {
                usuario.estado = false;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<Usuario> ObtenerPorEmailAsync(string mail)
        {
            return await _context.Usuarios.FirstOrDefaultAsync(u => u.mail == mail);
        }
        public async Task CrearAsync(Usuario usuario)
        {
            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();
            
        }
        public async Task<Usuario?> GetByMailWithCuentasAsync(string mail)
           => await _context.Usuarios
                        .Include(u => u.Cuentas)
                        .FirstOrDefaultAsync(u => u.mail == mail);
    }
}
