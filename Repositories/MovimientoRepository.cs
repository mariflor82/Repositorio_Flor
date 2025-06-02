using digitalArsv1.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static MovimientoDetalleDTO;

namespace digitalArsv1.Repositories
{
    public class MovimientoRepository : Repository<Movimiento>, IMovimientoRepository
    {
        private readonly DigitalArsContext _context;

        public MovimientoRepository(DigitalArsContext context) : base(context)
        {
            _context = context;
        }

        // 1️⃣ Agrega un nuevo Movimiento al contexto (no persiste hasta llamar a SaveAsync)
        public async Task CrearAsync(Movimiento movimiento)
        {
            _context.Movimientos.Add(movimiento);
        }

        // 2️⃣ Persiste todos los cambios pendientes (incluidos movimientos agregados)
        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }

        // 3️⃣ Devuelve el valor máximo actual de id_trx (o 0 si no hay movimientos)
        public async Task<int> GetMaxIdAsync()
        {
            var existeAlguno = await _context.Movimientos.AnyAsync();
            if (!existeAlguno)
                return 0;

            return await _context.Movimientos.MaxAsync(m => m.id_trx);
        }

        // 4️⃣ Método para traer todos los movimientos con sus relaciones
        public async Task<IEnumerable<Movimiento>> GetAllWithRelationsAsync()
        {
            return await _context.Movimientos
                .Include(m => m.CuentaOrig)
                .Include(m => m.CuentaDest)
                .Include(m => m.Transaccion)
                .ToListAsync();
        }

        // 5️⃣ Obtener último detalle de un movimiento por ID (incluye descripción)
        public async Task<MovimientoDetalleDTO> GetMovimientoDetallePorIdAsync(int id)
        {
            var mov = await _context.Movimientos
                .Include(m => m.Transaccion)
                .FirstOrDefaultAsync(m => m.id_trx == id);

            if (mov == null)
                return null!; // O bien arrojar excepción si prefieres

            return new MovimientoDetalleDTO
            {
                IdTrx = mov.id_trx,
                Fecha = mov.fecha,
                Monto = mov.monto,
                NroCuentaOrigen = mov.nro_cuenta_orig,
                NroCuentaDestino = mov.nro_cuenta_dest,
                DescripcionTransaccion = mov.Transaccion?.descripcion ?? string.Empty
            };
        }

        // Obtener los últimos  movimientos (desc) para el resumen del cliente 
        public async Task<List<MovimientoDetalleDTO>> GetResumenMovimientosPorClienteAsync(
           int nroCliente, DateTime desde, DateTime hasta)
        {
            // Convertir al rango que cubre cada día completo
            var startOfDay = desde.Date;              // 2025-06-02 00:00:00
            var endOfNextDay = hasta.Date.AddDays(1); // 2025-06-03 00:00:00

            // Obtener todos los números de cuenta de este cliente
            var cuentasPropias = await _context.Cuentas
                .Where(c => c.nro_cliente == nroCliente)
                .Select(c => c.nro_cuenta)
                .ToListAsync();

            // Filtrar movimientos donde origen o destino estén en esas cuentas y dentro del rango de días
            var query = _context.Movimientos
                .Include(m => m.Transaccion)
                .Where(m =>
                    ((m.nro_cuenta_orig.HasValue && cuentasPropias.Contains(m.nro_cuenta_orig.Value)) ||
                     (m.nro_cuenta_dest.HasValue && cuentasPropias.Contains(m.nro_cuenta_dest.Value)))
                    && m.fecha >= startOfDay
                    && m.fecha < endOfNextDay
                )
                .OrderByDescending(m => m.fecha)
                .Take(10);

            var lista = await query.ToListAsync();

            // Mapear a DTO
            return lista.Select(mov => new MovimientoDetalleDTO
            {
                IdTrx = mov.id_trx,
                Fecha = mov.fecha,
                Monto = mov.monto,
                NroCuentaOrigen = mov.nro_cuenta_orig,
                NroCuentaDestino = mov.nro_cuenta_dest,
                DescripcionTransaccion = mov.Transaccion?.descripcion ?? string.Empty
            }).ToList();
        }

        // Obtener los últimos movimientos (desc) pantalla principal
        public async Task<List<UltimosMovimientoDTO>> GetUltimosMovimientosPorClienteAsync(
            int nroCliente, DateTime desde, DateTime hasta)
        {
            // Convertir al rango que cubre cada día completo
            var startOfDay = desde.Date;              // 2025-06-02 00:00:00
            var endOfNextDay = hasta.Date.AddDays(1); // 2025-06-03 00:00:00

            // Obtener todos los números de cuenta de este cliente
            var cuentasPropias = await _context.Cuentas
                .Where(c => c.nro_cliente == nroCliente)
                .Select(c => c.nro_cuenta)
                .ToListAsync();

            // Filtrar movimientos donde origen o destino estén en esas cuentas y dentro del rango de días
            var query = _context.Movimientos
                .Include(m => m.Transaccion)
                .Where(m =>
                    ((m.nro_cuenta_orig.HasValue && cuentasPropias.Contains(m.nro_cuenta_orig.Value)) ||
                     (m.nro_cuenta_dest.HasValue && cuentasPropias.Contains(m.nro_cuenta_dest.Value)))
                    && m.fecha >= startOfDay
                    && m.fecha < endOfNextDay
                )
                .OrderByDescending(m => m.fecha)
                .Take(10);

            var lista = await query.ToListAsync();

            // Mapear a DTO
            return lista.Select(mov => new UltimosMovimientoDTO
            {
                Fecha = mov.fecha,
                DescripcionTransaccion = mov.Transaccion?.descripcion ?? string.Empty,
                Monto = mov.monto
            }).ToList();
        }

        // Obtener cuenta por número de cuenta
        public async Task<Cuenta> ObtenerCuentaPorNumeroAsync(int numeroCuenta)
        {
            return await _context.Cuentas
                .FirstOrDefaultAsync(c => c.nro_cuenta == numeroCuenta);
        }




    }
}
