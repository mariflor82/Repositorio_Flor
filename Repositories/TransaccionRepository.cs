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

    public async Task CrearAsync(Transaccion transaccion)
    {
        // Aquí esperamos que 'transaccion.codigo_transaccion' ya venga con el valor deseado (p.ej. 3).
        _context.Transacciones.Add(transaccion);
    }

    public async Task SaveAsync()
    {
        await _context.SaveChangesAsync();
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
}
