using digitalArsv1.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace digitalArsv1.Repositories
{
    public class MovimientoRepository : Repository<Movimiento>, IMovimientoRepository
    {
        private readonly DigitalArsContext _context;

        public MovimientoRepository(DigitalArsContext context) : base(context)
        {
            _context = context;
        }

        // Método para traer todos los movimientos 
        public async Task<IEnumerable<Movimiento>> GetAllWithRelationsAsync()
        {
            return await _context.Movimientos
                .Include(m => m.CuentaOrig)
                .Include(m => m.CuentaDest)
                .Include(m => m.Transaccion)
                .ToListAsync();
        }
        // Agrega un nuevo Movimiento al contexto (no persiste hasta llamar a SaveAsync).
       
        public async Task CrearAsync(Movimiento movimiento)
        {
            _context.Movimientos.Add(movimiento);
            // No se invoca SaveChangesAsync() aquí; el controlador llamará a SaveAsync()
        }

        
        /// guarda todos los cambios pendientes en el contexto (incluidos movimientos agregados).
       
        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }
        // ✅ NUEVO: Devuelve el valor máximo actual de id_trx (o 0 si no existen movimientos)
        public async Task<int> GetMaxIdAsync()
        {
         // Si no hay ningún registro, devolvemos 0; de lo contrario, el máximo id_trx
            var existeAlguno = await _context.Movimientos.AnyAsync();
            if (!existeAlguno)
                return 0;

         // EF Core traducirá esto a SELECT MAX(id_trx) FROM Movimiento
            return await _context.Movimientos.MaxAsync(m => m.id_trx);
        }

        //Obtener cuenta por número de cuenta(incluye validación de existencia)
        public async Task<Cuenta> ObtenerCuentaPorNumeroAsync(int numeroCuenta)
        {
            return await _context.Cuentas.FirstOrDefaultAsync(c => c.nro_cuenta == numeroCuenta);
        }

        

        // Agregar un nuevo movimiento y guardar cambios
        public async Task AddAsync(Movimiento movimiento)
        {
            await _context.Movimientos.AddAsync(movimiento);
            await _context.SaveChangesAsync();
        }







    }
}