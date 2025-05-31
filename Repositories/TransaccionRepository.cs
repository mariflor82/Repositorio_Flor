using digitalArsv1.Models;
using digitalArsv1.Repositories;
using digitalArsv1;

public class TransaccionRepository : Repository<Transaccion>, ITransaccionRepository
{
    private readonly DigitalArsContext _context;

    public TransaccionRepository(DigitalArsContext context) : base(context)
    {
        _context = context;
    }

    
    public async Task UpdateAsync(Transaccion transaccion)
    {
        Update(transaccion);  
        await SaveAsync();    
    }

    public async Task DeleteAsync(int id)
    {
        var transaccion = await _context.Transacciones.FindAsync(id);
        if (transaccion == null) return;

        Delete(transaccion);
        await SaveAsync();
    }
    // ✅ NUEVO: Agrega un nuevo registro de Transaccion al contexto (no Guarda hasta llamar a SaveAsync)
    public async Task CrearAsync(Transaccion transaccion)
    {
        _context.Transacciones.Add(transaccion);
        // No se invoca SaveChangesAsync() aquí; el controlador llamará a SaveAsync()
    }

    // ✅ NUEVO: Guarda todos los cambios pendientes en el contexto (incluidas las Transacciones añadidas)
    public async Task SaveAsync()
    {
        await _context.SaveChangesAsync();
    }
    // Agregar una nueva transacción y guardar la descripción
    public async Task AddAsync(Transaccion transaccion)
    {
        await _context.Transacciones.AddAsync(transaccion);
        await _context.SaveChangesAsync();
    }


}