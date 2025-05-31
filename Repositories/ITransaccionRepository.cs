using digitalArsv1.Models;
using digitalArsv1.Repositories;
using Microsoft.Win32;

public interface ITransaccionRepository : IRepository<Transaccion>
{
    Task UpdateAsync(Transaccion transaccion);
    Task DeleteAsync(int id);

    // Otros métodos específicos que quieras agregar

    Task CrearAsync(Transaccion transaccion);
    Task SaveAsync();


    //Agregar una nueva transacción(registro de movimiento financiero)
    Task AddAsync(Transaccion transaccion);
}