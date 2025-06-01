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
    private readonly ICuentaRepository _cuentaRepository;                             // **CAMBIO: agregado campo para repositorio de cuentas**
    private readonly IMovimientoRepository _movimientoRepository;
    private readonly ITransaccionRepository _transaccionRepository;

    // ✅ CAMBIO: Inyectamos ICuentaRepository además de los demás repositorios
    public MovimientosController(
        ICuentaRepository cuentaRepository,                                           // **CAMBIO: inyectamos ICuentaRepository**
        IMovimientoRepository movimientoRepository,
        ITransaccionRepository transaccionRepository)
    {
        _cuentaRepository = cuentaRepository;                                         // **CAMBIO: asignamos la dependencia de cuentas**
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

    [Authorize(Roles = "Billetera")]
    [HttpPost("movimiento")]
    public async Task<ActionResult> ProcesarMovimiento([FromBody] MovimientoDTO dto)
    {
        // 1) Validar que el DTO no sea null
        if (dto == null)
            return BadRequest("El cuerpo de la petición (MovimientoDTO) no puede ir vacío.");

        // 2) Validar que venga Descripción, Monto, etc.
        if (string.IsNullOrWhiteSpace(dto.Descripcion))
            return BadRequest("La descripción es obligatoria.");

        if (dto.Monto <= 0)
            return BadRequest("El monto debe ser mayor que cero.");

        // 3) Extraer nro_cliente del JWT
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? User.FindFirstValue("sub");
        if (!int.TryParse(userId, out var nroCliente))
            return Forbid("No se pudo extraer el cliente del token.");

        // 4) Determinar código de transacción a partir de dto.Descripción
        int codigoTransaccion;
        switch (dto.Descripcion.Trim())
        {
            case "Crédito por transferencia": codigoTransaccion = 1; break;
            case "Transferencia a otra cuenta": codigoTransaccion = 2; break;
            case "Depósito cuenta propia": codigoTransaccion = 3; break;
            case "Compra en comercio": codigoTransaccion = 4; break;
            case "Recarga de saldo virtual": codigoTransaccion = 5; break;
            default:
                return BadRequest("Descripción de transacción no reconocida.");
        }

        // 5) Si la transacción implica CuentaOrigen ≠ 0, la traemos de BD
        Cuenta cuentaOrigen = null!;
        if (dto.NroCuentaOrigen > 0)
        {
            cuentaOrigen = await _cuentaRepository.GetByIdWithUsuarioAsync(dto.NroCuentaOrigen);
            if (cuentaOrigen == null)
                return NotFound($"La cuenta de origen {dto.NroCuentaOrigen} no existe en la base.");
            if (cuentaOrigen.nro_cliente != nroCliente)
                return Forbid("No tienes permiso sobre la cuenta de origen.");
        }

        // 6) Si la transacción implica CuentaDestino ≠ null, la traemos
        Cuenta cuentaDestino = null!;
        if (dto.NroCuentaDestino.HasValue)
        {
            cuentaDestino = await _cuentaRepository.GetByIdWithUsuarioAsync(dto.NroCuentaDestino.Value);
            if (cuentaDestino == null)
                return NotFound($"La cuenta de destino {dto.NroCuentaDestino.Value} no existe en la base.");

            // Para transacción 1,2,3 validamos que la cuenta destino sea del cliente autenticado
            if ((codigoTransaccion == 1 || codigoTransaccion == 2 || codigoTransaccion == 3)
                && cuentaDestino.nro_cliente != nroCliente)
            {
                return Forbid("La cuenta de destino no pertenece al cliente autenticado.");
            }
        }

        // 7) Verificar saldo si se va a debitar (transacciones 2,4,5)
        if (codigoTransaccion == 2 || codigoTransaccion == 4 || codigoTransaccion == 5)
        {
            if (cuentaOrigen == null)
                return BadRequest("La cuenta de origen debe especificarse para esta transacción.");
            var saldoOrigen = await _cuentaRepository.ObtenerSaldoAsync(cuentaOrigen.nro_cuenta);
            if (dto.Monto > saldoOrigen)
                return BadRequest("Saldo insuficiente en la cuenta de origen.");
        }

        // 8) Actualizar saldos según el tipo de operación
        switch (codigoTransaccion)
        {
            case 1: // Crédito por transferencia: el saldo se reflejará cuando consultemos movimientos
                    // (no se hace nada aquí sobre Cuenta; el INSERT en Movimiento ya crea el registro)
                break;

            case 2: // Transferencia a otra cuenta
                    // (tampoco modificamos Cuenta directamente; el INSERT en Movimiento hará el efecto de debitar/origin y acreditar/destino)
                break;

            case 3: // Depósito a cuenta propia
                    // (mismo criterio: no modificamos Cuenta; el INSERT en Movimiento sumará en la cuenta destino)
                break;

            case 4: // Compra en comercio
                    // (igual: dejamos que el INSERT en Movimiento refleje la resta de la cuenta origen)
                break;

            case 5: // Recarga de saldo virtual
                    // (igualmente, confiamos en el INSERT en Movimiento para restar de la cuenta origen)
                break;
        }

        // 9) Registrar o actualizar la tabla Transaccion
        var transacExistente = await _transaccionRepository.GetByIdAsync(codigoTransaccion);
        if (transacExistente != null)
        {
            transacExistente.descripcion = dto.Descripcion;
            await _transaccionRepository.SaveAsync();
        }
        else
        {
            var nuevaTransac = new Transaccion
            {
                codigo_transaccion = codigoTransaccion,
                descripcion = dto.Descripcion
            };
            await _transaccionRepository.CrearAsync(nuevaTransac);
            await _transaccionRepository.SaveAsync();
        }

        // 10) Generar el siguiente id_trx (máximo + 1)
        var maxId = await _movimientoRepository.GetMaxIdAsync();
        var nuevoId = maxId + 1;

        // 11) Crear y guardar MOVIMIENTOS según tipo de transacción
        if (codigoTransaccion == 2 && dto.NroCuentaOrigen > 0 && dto.NroCuentaDestino.HasValue)
        {
            // TRANSFERENCIA INTERNA:  
            //   1) CREAR Movimiento de crédito (código 1) para sumar en la cuenta destino  
            var movCredito = new Movimiento
            {
                id_trx = nuevoId,
                codigo_transaccion = 1,                    // “Crédito por transferencia”
                nro_cuenta_orig = null,                    // origen nulo (solo destino)
                nro_cuenta_dest = dto.NroCuentaDestino.Value,
                monto = dto.Monto,
                fecha = System.DateTime.UtcNow
            };
            await _movimientoRepository.CrearAsync(movCredito);

            //   2) CREAR Movimiento de débito (código 2) para restar en la cuenta origen  
            var movDebito = new Movimiento
            {
                id_trx = nuevoId + 1,
                codigo_transaccion = 2,                    // “Transferencia a otra cuenta”
                nro_cuenta_orig = dto.NroCuentaOrigen,
                nro_cuenta_dest = null,                    // destino nulo (solo origen)
                monto = dto.Monto,
                fecha = System.DateTime.UtcNow
            };
            await _movimientoRepository.CrearAsync(movDebito);

            // Finalmente, persistimos ambos movimientos en bloque
            await _movimientoRepository.SaveAsync();
        }
        else if (codigoTransaccion == 4 || codigoTransaccion == 5)
        {
            // COMPRA o RECARGA: un solo movimiento de débito
            var movUnico = new Movimiento
            {
                id_trx = nuevoId,
                codigo_transaccion = codigoTransaccion,       // 4 = Compra en comercio   5 = Recarga de saldo virtual
                nro_cuenta_orig = dto.NroCuentaOrigen,        // debita la cuenta origen
                nro_cuenta_dest = null,                       // no hay cuenta destino
                monto = dto.Monto,
                fecha = System.DateTime.UtcNow
            };
            await _movimientoRepository.CrearAsync(movUnico);
            await _movimientoRepository.SaveAsync();
        }
        else
        {
            // Resto de códigos (1 o 3), usamos un único movimiento:
            //   1 = “Crédito por transferencia”  (suma en destino)  
            //   3 = “Depósito cuenta propia”     (suma en destino)  
            var mov = new Movimiento
            {
                id_trx = nuevoId,
                codigo_transaccion = codigoTransaccion,
                nro_cuenta_orig = null,                    // no hay débito para estos casos
                nro_cuenta_dest = dto.NroCuentaDestino,    // destino incluye la cuenta a acreditar
                monto = dto.Monto,
                fecha = System.DateTime.UtcNow
            };
            await _movimientoRepository.CrearAsync(mov);
            await _movimientoRepository.SaveAsync();
        }

        return Ok(new { mensaje = "Movimiento(s) registrado(s) correctamente." });
    }
    // ==== MÉTODO PARA TRANSFERENCIA ENTRE CUENTAS
    // =============================================
    [Authorize(Roles = "Billetera")]
    [HttpPost("transferir")]
    public async Task<IActionResult> Transferir([FromBody] TransferenciaDTO dto)
    {
        // 1️⃣ Validar que venga el DTO completo
        if (dto == null)
            return BadRequest("El cuerpo de la petición (TransferenciaDTO) es obligatorio.");

        if (dto.Monto <= 0)
            return BadRequest("El monto debe ser mayor que cero.");

       

        // 2️⃣ Extraer nro_cliente del JWT (para verificar propiedad de la cuenta ORIGEN)
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? User.FindFirstValue("sub");
        if (!int.TryParse(userId, out var nroCliente))
            return Forbid("Token inválido.");

        // 3️⃣ Obtener CUENTA DE ORIGEN según el DTO y verificar que pertenezca al usuario logueado
        var cuentaOrigen = await _cuentaRepository.GetByIdWithUsuarioAsync(dto.NroCuentaOrigen);
        if (cuentaOrigen == null)
            return NotFound($"La cuenta de origen {dto.NroCuentaOrigen} no existe.");

        if (cuentaOrigen.nro_cliente != nroCliente)
            return Forbid("No tienes permiso sobre la cuenta de origen.");

        // 4️⃣ Verificar saldo suficiente en cuenta ORIGEN
        var saldoOrigen = await _cuentaRepository.ObtenerSaldoAsync(cuentaOrigen.nro_cuenta);
        if (dto.Monto > saldoOrigen)
            return BadRequest("Saldo insuficiente en la cuenta de origen.");

        // 5️⃣ Intentar OBTENER CUENTA DESTINO; si no existe, marcamos “existeDestino = false”
        bool existeDestino = true;
        var cuentaDestino = await _cuentaRepository.GetByIdWithUsuarioAsync(dto.NroCuentaDestino);
        if (cuentaDestino == null)
        {
            existeDestino = false;
        }
        else
        {
            // Opcional: evitar transferir a la misma cuenta
            if (cuentaDestino.nro_cuenta == cuentaOrigen.nro_cuenta)
                return BadRequest("La cuenta destino no puede coincidir con la de origen.");
        }

        // 6️⃣ Asegurar que los códigos 2 y 1 existan en Transaccion
        var transDebito = await _transaccionRepository.GetByIdAsync(2);
        if (transDebito == null)
        {
            transDebito = new Transaccion
            {
                codigo_transaccion = 2,
                descripcion = "Transferencia a otra cuenta"
            };
            await _transaccionRepository.CrearAsync(transDebito);
            await _transaccionRepository.SaveAsync();
        }

        var transCredito = await _transaccionRepository.GetByIdAsync(1);
        if (transCredito == null)
        {
            transCredito = new Transaccion
            {
                codigo_transaccion = 1,
                descripcion = "Crédito por transferencia"
            };
            await _transaccionRepository.CrearAsync(transCredito);
            await _transaccionRepository.SaveAsync();
        }

        // 7️⃣ Generar IDs consecutivos para ambos movimientos
        var maxId = await _movimientoRepository.GetMaxIdAsync();
        var idDebito = maxId + 1;
        var idCredito = idDebito + 1;

        // 8️⃣ Crear Movimiento de DÉBITO en ORIGEN (código 2)
        var movDebito = new Movimiento
        {
            id_trx = idDebito,
            fecha = DateTime.UtcNow,
            monto = dto.Monto,
            nro_cuenta_orig = cuentaOrigen.nro_cuenta,
            nro_cuenta_dest = existeDestino ? dto.NroCuentaDestino : (int?)null,
            codigo_transaccion = 2
        };
        // Ajustar saldo en la cuenta ORIGEN
        cuentaOrigen.saldo -= dto.Monto;

        // 9️⃣ Crear Movimiento de CRÉDITO en DESTINO (código 1), solo si existeDestino
        Movimiento? movCredito = null;
        if (existeDestino)
        {
            movCredito = new Movimiento
            {
                id_trx = idCredito,
                fecha = DateTime.UtcNow,
                monto = dto.Monto,
                // Para el registro de crédito, usamos “nro_cuenta_orig” = misma cuenta destino
                nro_cuenta_orig = dto.NroCuentaDestino,
                nro_cuenta_dest = cuentaDestino.nro_cuenta,
                codigo_transaccion = 1
            };
            // Ajustar saldo en la cuenta DESTINO
            cuentaDestino.saldo += dto.Monto;
        }

        // 🔟 Guardar ambos Movimientos en bloque
        await _movimientoRepository.CrearAsync(movDebito);
        if (movCredito != null)
            await _movimientoRepository.CrearAsync(movCredito);

        // 1️⃣1️⃣ Guardar cambios en Movimientos y luego en Cuentas (para persistir saldos)
        await _movimientoRepository.SaveAsync();
        await _cuentaRepository.SaveAsync();

        return Ok(new
        {
            idDebito = movDebito.id_trx,
            idCredito = movCredito?.id_trx,
            mensaje = "Transferencia procesada correctamente."
        });
    }






    // POST /api/Movimientos/depositar
    [Authorize(Roles = "Billetera")]
    [HttpPost("depositar")]
    public async Task<ActionResult> Depositar([FromBody] DepositoDTO dto)
    
        {
            // 1) Validar que el DTO no sea null
            if (dto == null)
                return BadRequest("El cuerpo de la petición no puede estar vacío.");

            // 2) Validar que llegue la descripción y el monto
            if (string.IsNullOrWhiteSpace(dto.Descripcion))
                return BadRequest("La descripción es obligatoria.");
            if (dto.Monto <= 0)
                return BadRequest("El monto debe ser mayor que cero.");

            // 3) Extraer nro_cliente desde el JWT (para determinar quién hace el depósito)
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? User.FindFirstValue("sub");
            if (!int.TryParse(userId, out var nroCliente))
                return Forbid("Token inválido.");  // 🔴 Ya no validamos cuentaDestino aquí

            // 4) Verificar que la cuenta destino exista y pertenezca al cliente autenticado
            var cuentaDestino = await _cuentaRepository.GetByIdWithUsuarioAsync(dto.NroCuentaDestino);
            if (cuentaDestino == null)
                return NotFound($"La cuenta destino {dto.NroCuentaDestino} no existe.");
            if (cuentaDestino.nro_cliente != nroCliente)
                return Forbid("No tienes permiso sobre la cuenta destino.");

            // 5) OBTENER O CREAR la Transacción "Depósito cuenta propia" (código = 3)
            const int CODIGO_DEPOSITO = 3;
            var transac = await _transaccionRepository.GetByIdAsync(CODIGO_DEPOSITO);
            if (transac != null)
            {
                // ✅ SI EXISTE, actualizamos la descripción (puede cambiarse si el front envía un texto distinto)
                transac.descripcion = dto.Descripcion;
                await _transaccionRepository.SaveAsync();
            }
            else
            {
                // ✅ SI NO EXISTE, lo creamos con código = 3
                var nuevaTransac = new Transaccion
                {
                    codigo_transaccion = CODIGO_DEPOSITO,
                    descripcion = dto.Descripcion
                };
                await _transaccionRepository.CrearAsync(nuevaTransac);
                await _transaccionRepository.SaveAsync();
            }

            // 6) Generar el siguiente id_trx (máximo + 1)
            var maxId = await _movimientoRepository.GetMaxIdAsync();
            var nuevoId = maxId + 1;

            // 7) Crear el objeto Movimiento solo para la cuenta destino
            var mov = new Movimiento
            {
                id_trx = nuevoId,
                codigo_transaccion = CODIGO_DEPOSITO,
                nro_cuenta_orig = null,                     // 🔴 Para depósito, origen = null
                nro_cuenta_dest = dto.NroCuentaDestino,     // ✅ Solo destino
                monto = dto.Monto,
                fecha = DateTime.UtcNow
            };

            // 8) Acreditar directamente el monto en la cuenta destino
            cuentaDestino.saldo += dto.Monto;
            //     (No hace falta restar nada en origen, porque es un depósito externo)

            // 9) Guardar el movimiento
            await _movimientoRepository.CrearAsync(mov);
            await _movimientoRepository.SaveAsync();

            return Ok(new { id = mov.id_trx, mensaje = "Depósito registrado correctamente." });
        }

    }











/*
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

    return Ok(new { mensaje = "Movimiento procesado correctamente." });*/


