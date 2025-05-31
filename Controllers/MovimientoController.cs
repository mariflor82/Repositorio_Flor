using digitalArsv1.DTOs;
using digitalArsv1.Models;
using digitalArsv1.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class MovimientosController : ControllerBase
{
    private readonly ICuentaRepository _cuentaRepository;
    private readonly Random _rng = new();
    private readonly IMovimientoRepository _movimientoRepository;
    private readonly ITransaccionRepository _transaccionRepository;


    // ✅ CAMBIO: Inyectamos ITransaccionRepository para gestionar transacciones
    public MovimientosController(
        IMovimientoRepository movimientoRepository,
        ITransaccionRepository transaccionRepository)
    {
        _movimientoRepository = movimientoRepository;
        _transaccionRepository = transaccionRepository;
    }

    [HttpGet]  // Obtiene la lista de movimientos
    public async Task<ActionResult<IEnumerable<Movimiento>>> GetMovimientos()
    {
        var movimientos = await _movimientoRepository.GetAllWithRelationsAsync();
        return Ok(movimientos);
    }

    [HttpGet("{id}")] // Busca un movimiento por su id (id_trx)
    public async Task<ActionResult<Movimiento>> GetMovimiento(int id)
    {
        var movimiento = await _movimientoRepository.GetByIdAsync(id);
        if (movimiento == null)
            return NotFound();

        return Ok(movimiento);
    }
    


    [HttpPut("{id}")]// Actualiza un movimiento existente
    public async Task<IActionResult> PutMovimiento(int id, Movimiento movimiento)
    {
        if (id != movimiento.id_trx)
            return BadRequest();

        _movimientoRepository.Update(movimiento);
        await _movimientoRepository.SaveAsync();

        return NoContent();
    }

    [HttpDelete("{id}")] // Elimina un movimiento por id
    public async Task<IActionResult> DeleteMovimiento(int id)
    {
        var movimiento = await _movimientoRepository.GetByIdAsync(id);
        if (movimiento == null)
            return NotFound();

        _movimientoRepository.Delete(movimiento);
        await _movimientoRepository.SaveAsync();

        return NoContent();
    }

    [HttpPost("registrar")] // RegistrarMovimiento: lógica completa según tipo de transacción
    [Authorize(Roles = "Billetera")]

    public async Task<ActionResult> ProcesarMovimiento([FromBody] MovimientoDTO movimientoDto)
    {
        // 1️⃣ Validar que la descripción venga presente
        if (string.IsNullOrWhiteSpace(movimientoDto.Descripcion))
            return BadRequest("La descripción de la transacción es obligatoria.");

        // 2️⃣ Asignar el código de transacción según la descripción
        int codigoTransaccion;
        switch (movimientoDto.Descripcion)
        {
            case "Crédito por transferencia":
                codigoTransaccion = 1;
                break;
            case "Transferencia a otra cuenta":
                codigoTransaccion = 2;
                break;
            case "Depósito cuenta propia":
                codigoTransaccion = 3;
                break;
            case "Compra en comercio":
                codigoTransaccion = 4;
                break;
            case "Recarga de saldo virtual":
                codigoTransaccion = 5;
                break;
            default:
                return BadRequest("Descripción de transacción no reconocida.");
        }

        // 3️⃣ Validar que el monto sea positivo
        if (movimientoDto.Monto <= 0)
            return BadRequest("El monto debe ser mayor a cero.");

        // 4️⃣ Extraer nro_cliente del JWT
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? User.FindFirstValue("sub");
        if (!int.TryParse(userId, out var clienteAutenticado))
            return Forbid(); // usuario autenticado, pero no encontramos el claim; cortamos

        // 5️⃣ Verificar reglas de origen/destino según código
        if (codigoTransaccion == 3) // Depósito
        {
            if (movimientoDto.NroCuentaOrigen != 0 || !movimientoDto.NroCuentaDestino.HasValue)
                return BadRequest("Depósito requiere Origen = 0 y Destino válido.");
        }
        else if (codigoTransaccion == 2 || codigoTransaccion == 1) // Transferencias/Crédito
        {
            if (movimientoDto.NroCuentaOrigen <= 0 || !movimientoDto.NroCuentaDestino.HasValue)
                return BadRequest("Transferencia o crédito requiere Origen != 0 y Destino válido.");
        }
        else if (codigoTransaccion == 4 || codigoTransaccion == 5) // Compra o Recarga
        {
            if (movimientoDto.NroCuentaOrigen <= 0 || movimientoDto.NroCuentaDestino != null)
                return BadRequest("Compra/Recarga requiere Origen != 0 y Destino = null.");
        }

        // 6️⃣ Obtener y validar cuentas de origen/destino
        Cuenta cuentaOrigen = null!;
        if (movimientoDto.NroCuentaOrigen != 0)
        {
            cuentaOrigen = await _cuentaRepository.GetByIdWithUsuarioAsync(movimientoDto.NroCuentaOrigen);
            if (cuentaOrigen == null || cuentaOrigen.nro_cliente != clienteAutenticado)
                return Forbid("No tiene permiso sobre la cuenta de origen.");

            // Verificar saldo para retiros, transferencias, compras y recargas
            var saldoOrigen = await _cuentaRepository.ObtenerSaldoAsync(movimientoDto.NroCuentaOrigen);
            if ((codigoTransaccion == 1 || codigoTransaccion == 2 || codigoTransaccion == 4 || codigoTransaccion == 5)
                && movimientoDto.Monto > saldoOrigen)
            {
                return BadRequest("Saldo insuficiente en cuenta de origen.");
            }
        }

        Cuenta cuentaDestino = null!;
        if (movimientoDto.NroCuentaDestino.HasValue)
        {
            cuentaDestino = await _cuentaRepository.GetByIdWithUsuarioAsync(movimientoDto.NroCuentaDestino.Value);
            if (cuentaDestino == null)
                return NotFound($"Cuenta destino {movimientoDto.NroCuentaDestino.Value} no existe.");

            // Verificar que para crédito y depósito la cuenta destino pertenezca al cliente autenticado
            if ((codigoTransaccion == 1 || codigoTransaccion == 2 || codigoTransaccion == 3)
                && cuentaDestino.nro_cliente != clienteAutenticado)
            {
                return Forbid("La cuenta de destino no pertenece al cliente autenticado.");
            }
        }

        // 7️⃣ Ajustar saldos según tipo
        if (codigoTransaccion == 1) // Crédito por transferencia
        {
            cuentaDestino.saldo += movimientoDto.Monto;
        }
        else if (codigoTransaccion == 2) // Transferencia
        {
            cuentaOrigen.saldo -= movimientoDto.Monto;
            cuentaDestino.saldo += movimientoDto.Monto;
        }
        else if (codigoTransaccion == 3) // Depósito cuenta propia
        {
            cuentaDestino.saldo += movimientoDto.Monto;
        }
        else if (codigoTransaccion == 4) // Compra en comercio
        {
            cuentaOrigen.saldo -= movimientoDto.Monto;
        }
        else if (codigoTransaccion == 5) // Recarga de saldo virtual
        {
            cuentaOrigen.saldo -= movimientoDto.Monto;
        }

        // 8️⃣ Crear y guardar nuevo registro en Transaccion
        var nuevaTransaccion = new Transaccion
        {
            codigo_transaccion = codigoTransaccion,     // **CAMBIO: asignar el código aquí (ya no puede ser NULL)**
            descripcion = movimientoDto.Descripcion     // **CAMBIO: grabar la descripción**
        };
        await _transaccionRepository.CrearAsync(nuevaTransaccion);
        await _transaccionRepository.SaveAsync();

        // 9️⃣ Generar el próximo id_trx
        int nextId = await _movimientoRepository.GetMaxIdAsync() + 1;

        // 🔟 Crear y guardar nuevo Movimiento
        var mov = new Movimiento
        {
            id_trx = nextId,
            fecha = DateTime.UtcNow,
            monto = movimientoDto.Monto,
            nro_cuenta_orig = movimientoDto.NroCuentaOrigen,
            nro_cuenta_dest = movimientoDto.NroCuentaDestino,
            codigo_transaccion = codigoTransaccion        // **CAMBIO: colocar código en Movimiento**
        };

        await _movimientoRepository.CrearAsync(mov);
        await _movimientoRepository.SaveAsync();

        return Ok(new { mensaje = "Movimiento procesado correctamente." });
    }
}
