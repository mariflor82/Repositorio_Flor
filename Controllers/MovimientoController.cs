using digitalArsv1.DTOs;
using digitalArsv1.Models;
using digitalArsv1.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using static MovimientoDetalleDTO;

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

    // ==== Obtener detalles de un movimiento en particular ====

      // GET /api/Movimientos/{id}/detalle
        [HttpGet("{id}/detalle")]
        public async Task<ActionResult<MovimientoDetalleDTO>> GetDetalle(int id)
        {
            var detalle = await _movimientoRepository.GetMovimientoDetallePorIdAsync(id);
            if (detalle == null)
                return NotFound();

            return Ok(detalle);
        }

        // ==== Obtener últimos 10 movimientos de la cuenta del usuario logueado dentro de un rango ====
        // POST /api/Movimientos/ultimos/resumen
        [HttpPost("resumen")]
        public async Task<ActionResult<List<MovimientoDetalleDTO>>> GetResumenPorRango([FromBody] RangoFechasMovDTO rango)
        {
            // 1) Extraer nro_cliente del JWT
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? User.FindFirstValue("sub");
            if (!int.TryParse(userId, out var nroCliente))
                return Forbid();

            // 2) Llamar al repositorio para obtener los últimos 10 movimientos
            var lista = await _movimientoRepository.GetResumenMovimientosPorClienteAsync(
                nroCliente, rango.FechaDesde, rango.FechaHasta);

            return Ok(lista);
        }
    // ==== Obtener últimos movimientos de la cuenta del usuario logueado dentro de un rango para pantalla principal====
    // POST /api/Movimientos/ultimos/tres
    [HttpPost("ultimos")]
    public async Task<ActionResult<List<UltimosMovimientoDTO>>> GetUltimosPorRango([FromBody] RangoFechasMovDTO rango)
    {
        // 1) Extraer nro_cliente del JWT
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? User.FindFirstValue("sub");
        if (!int.TryParse(userId, out var nroCliente))
            return Forbid();

        // 2) Llamar al repositorio para obtener los últimos 10 movimientos
        var lista = await _movimientoRepository.GetUltimosMovimientosPorClienteAsync(
            nroCliente, rango.FechaDesde, rango.FechaHasta);

        return Ok(lista);
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

    // ==== MÉTODO PARA DEPOSITO EN CUENTA
    // =============================================
    // POST /api/Movimientos/depositar
    [Authorize(Roles = "Billetera")]
    [HttpPost("depositar")]
    public async Task<ActionResult> Depositar([FromBody] DepositoDTO dto)
    {
        // 1️⃣ Validar que el DTO no sea null
        if (dto == null)
            return BadRequest("El cuerpo de la petición no puede estar vacío.");

        // 2️⃣ Validar que el monto sea positivo
        if (dto.Monto <= 0)
            return BadRequest("El monto debe ser mayor que cero.");

        // 3️⃣ Extraer nro_cliente desde el JWT
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? User.FindFirstValue("sub");
        if (!int.TryParse(userId, out var nroCliente))
            return Forbid("Token inválido.");

        // 4️⃣ Obtener la(s) cuenta(s) del cliente autenticado
        var cuentasUsuario = await _cuentaRepository.GetByClienteAsync(nroCliente);
        if (cuentasUsuario == null || !cuentasUsuario.Any())
            return NotFound("No tienes ninguna cuenta. Primero abre una cuenta.");

        // → Asumimos que el depósito siempre va a la "primera" cuenta del usuario:
        var cuentaDestino = cuentasUsuario.First();

        // 5️⃣ Obtener o crear la Transacción “Depósito cuenta propia” (código = 3)
        const int CODIGO_DEPOSITO = 3;
        const string DESCRIPCION_DEPOSITO = "Depósito cuenta propia";

        // Intentamos leerla de la base
        var transac = await _transaccionRepository.GetByIdAsync(CODIGO_DEPOSITO);

        if (transac == null)
        {
            // Si no existe, la creamos con código y descripción
            transac = new Transaccion
            {
                codigo_transaccion = CODIGO_DEPOSITO,
                descripcion = DESCRIPCION_DEPOSITO
            };
            await _transaccionRepository.CrearAsync(transac);
            await _transaccionRepository.SaveAsync();
        }
        else if (transac.descripcion != DESCRIPCION_DEPOSITO)
        {
            // Si existe pero la descripción no coincide, la actualizamos
            transac.descripcion = DESCRIPCION_DEPOSITO;
            // NO necesitamos llamar a CrearAsync; solo marcamos la entidad como modificada:
            await _transaccionRepository.SaveAsync();
        }
        // Si ya existía con la descripción correcta, no hacemos nada adicional.

        // 6️⃣ Calcular el siguiente id_trx: (max(id_trx) + 1)
        //    Esto supone que tu repositorio GetMaxIdAsync() devuelve 0 si no hay movimientos.
        var maxId = await _movimientoRepository.GetMaxIdAsync();
        var nuevoIdTrx = maxId + 1;

        // 7️⃣ Crear el objeto Movimiento para la cuenta destino
        var mov = new Movimiento
        {
            id_trx = nuevoIdTrx,
            codigo_transaccion = CODIGO_DEPOSITO,
            nro_cuenta_orig = null,                     // Para depósito, origen = null
            nro_cuenta_dest = cuentaDestino.nro_cuenta, // Destino es la cuenta del usuario
            monto = dto.Monto,
            fecha = DateTime.UtcNow                     // Fecha UTC ahora
        };

        // 8️⃣ Acreditar directamente el monto en la cuenta destino
        cuentaDestino.saldo += dto.Monto;

        // 9️⃣ Guardar el movimiento y el saldo actualizado de la cuenta
        //    Primero creamos el movimiento:
        await _movimientoRepository.CrearAsync(mov);
        //    Luego guardamos ambos cambios (Movimiento y la cuenta modificada):
        await _movimientoRepository.SaveAsync();
        await _cuentaRepository.SaveAsync();

        // 🔟 Devolver resultado confirmando el id_trx recién creado
        return Ok(new
        {
            id_trx = mov.id_trx,
            mensaje = "Depósito registrado correctamente.",
            saldoActualizado = cuentaDestino.saldo
        });
    }


}



