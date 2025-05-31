using digitalArsv1.DTOs;
using digitalArsv1.Models;
using digitalArsv1.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Data;


namespace digitalArsv1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CuentasController : ControllerBase
    {
        private readonly ICuentaRepository _cuentaRepository;
        private readonly Random _rng = new();
        private readonly IConfiguration _config;
        private readonly IMovimientoRepository _movimientoRepository; // NUEVO: repositorio para Movimientos
        private readonly ITransaccionRepository _transaccionRepository;

        public CuentasController(
            ICuentaRepository cuentaRepository,
            IMovimientoRepository movimientoRepository,
            ITransaccionRepository transaccionRepository)
        {
            _cuentaRepository = cuentaRepository;
            _movimientoRepository = movimientoRepository;
            _transaccionRepository = transaccionRepository;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Cuenta>>> GetCuentas()
        {
            var cuentas = await _cuentaRepository.GetAllWithUsuarioAsync();
            return Ok(cuentas);
        }

        [HttpGet("{nroCuenta}/saldo")]
        public async Task<ActionResult<decimal>> ObtenerSaldo(int nroCuenta)
        {
            var saldo = await _cuentaRepository.ObtenerSaldoAsync(nroCuenta);
            return Ok(saldo);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Cuenta>> GetCuenta(int id)
        {
            var cuenta = await _cuentaRepository.GetByIdWithUsuarioAsync(id);
            if (cuenta == null)
                return NotFound();

            return Ok(cuenta);
        }


        // POST /api/Cuentas
        [Authorize(Roles = "Billetera")]
        [HttpPost]
        public async Task<ActionResult<CuentaConsultaDTO>> CrearCuenta()
        {
            // 1. Sacar el nro_cliente del JWT
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? User.FindFirstValue("sub");
            if (!int.TryParse(userId, out var nroCliente))
                return Forbid();

            // 2. Verificar si ya existe cuenta para ese usuario
            if (await _cuentaRepository.ExisteCuenta(nroCliente))
                return Conflict("El usuario ya tiene una cuenta.");

            // 3. Generar CBU
            const string prefijoCBU = "268006110208";
            var sufijo = string.Concat(
            Enumerable.Range(0, 10)
                     .Select(_ => _rng.Next(0, 10).ToString())
        );
            var cbu = prefijoCBU + sufijo;

            // 4. Crear la cuenta
            // El número de cuenta son los últimos 5 dígitos del CBU
            var ultimosCinco = cbu.Substring(cbu.Length - 5);       // Extraer últimos 5 dígitos
            var nroCuentaInt = int.Parse(ultimosCinco);
            var nuevaCuenta = new Cuenta

            {
                producto = "Caja de ahorro",
                CBU = prefijoCBU + sufijo,
                estado = true,
                nro_cliente = nroCliente,
                rol_cta = "Titular",
                nro_cuenta = nroCuentaInt
            };

            await _cuentaRepository.CrearAsync(nuevaCuenta);
            await _cuentaRepository.SaveAsync();

            // 5. Mapear a DTO
            var result = new CuentaConsultaDTO
            {
                NroCuenta = nuevaCuenta.nro_cuenta,
                Producto = nuevaCuenta.producto,
                CBU = nuevaCuenta.CBU,
                Estado = nuevaCuenta.estado,
                NroCliente = nuevaCuenta.nro_cliente,
                RolCta = nuevaCuenta.rol_cta
            };

            return CreatedAtAction(
                nameof(GetCuenta),
                new { id = result.NroCuenta },
                result);
        }

        // GET /api/Cuentas/por-cliente
        /// Devuelve solo NroCuenta, CBU, Nombre y Apellido del Usuario, y Saldo.
        /// </summary>
        [Authorize(Roles = "Billetera")]
        [HttpGet("por-cliente")]
        public async Task<ActionResult<IEnumerable<CuentaResumenDTO>>> GetCuentasPorCliente()
        {
            // 1. Extraer nro_cliente desde el JWT
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? User.FindFirstValue("sub");
            if (!int.TryParse(userId, out var nroCliente))
                return Forbid();

            // 2. Obtener todas las cuentas del cliente 
            //    (Incluye Usuario porque GetByClienteAsync hace Include internamente)
            var cuentas = await _cuentaRepository.GetByClienteAsync(nroCliente);

            // 3. Mapear a CuentaResumenDTO (si no hay cuentas, devolvemos lista vacía)
            var resumenList = new List<CuentaResumenDTO>(); // NUEVO: lista de resultados
            
            {
                foreach (var cuenta in cuentas)
                {
                    //3. Obtener saldo de forma asíncrona
                    var saldo = await _cuentaRepository.ObtenerSaldoAsync(cuenta.nro_cuenta);

                    // 4️. Agregar un nuevo DTO por cada cuenta
                    resumenList.Add(new CuentaResumenDTO
                    {
                        NroCuenta = cuenta.nro_cuenta,
                        CBU = cuenta.CBU ?? string.Empty,
                        Nombre = cuenta.Usuario?.nombre ?? string.Empty,
                        Apellido = cuenta.Usuario?.apellido ?? string.Empty,
                        Saldo = saldo

                    });
                }
            }

                    // 5 Devolver 200 OK con la lista (puede estar vacía)
                       return Ok(resumenList);
        }

        // POST /api/Cuentas/movimiento
        /// Recibe un MovimientoDTO y, según Descripción extrae el Código de Transacción:
        ///   1 = Retiro
        ///   2 = Depósito
        ///   3 = Transferencia
        ///   4 = Compra en comercio
        ///   5 = Recarga de saldo virtual
        ///
        /// Luego aplica la lógica correspondiente sobre saldos y guarda registro.
        /// </summary>
        [Authorize(Roles = "Billetera")]
        [HttpPost("movimiento")]
        public async Task<ActionResult> ProcesarMovimiento([FromBody] MovimientoDTO dto)
        {
            //  Validar monto positivo
            if (dto.Monto <= 0)
                return BadRequest("El monto debe ser mayor a cero.");

            //  Extraer nro_cliente del JWT
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? User.FindFirstValue("sub");
            if (!int.TryParse(userId, out var nroCliente))
                return Forbid();

            // **2️⃣ Obtener el código de transacción a partir de la descripción**
            var transac = (await _transaccionRepository.GetAllAsync())
                           .FirstOrDefault(t => t.descripcion == dto.Descripcion);
            if (transac == null)
                return BadRequest();
            int codigoTransaccion = transac.codigo_transaccion;  // **CAMBIO**

            // **3️⃣ Validar esquema de cuentas según el código obtenido**
            if (codigoTransaccion == 3)
            {
                // Transferencia
                if (dto.NroCuentaOrigen == 0 || dto.NroCuentaDestino == null)
                    return BadRequest("Para transferencia, debe especificar Origen != 0 y Destino != null.");
            }
            else if (codigoTransaccion == 2)
            {
                // Depósito
                if (dto.NroCuentaOrigen != 0 || dto.NroCuentaDestino == null)
                    return BadRequest("Para depósito, Origen debe ser 0 y Destino != null.");
            }
            else if (codigoTransaccion == 1)
            {
                // Retiro
                if (dto.NroCuentaOrigen == 0 || dto.NroCuentaDestino != null)
                    return BadRequest("Para retiro, Origen != 0 y Destino debe ser null.");
            }
            else if (codigoTransaccion == 4)
            {
                // Compra en comercio
                if (dto.NroCuentaOrigen <= 0 || dto.NroCuentaDestino != null)
                    return BadRequest("Para compra, Origen != 0 y Destino debe ser null.");
            }
            else if (codigoTransaccion == 5)
            {
                // Recarga de saldo virtual
                if (dto.NroCuentaOrigen <= 0 || dto.NroCuentaDestino != null)
                    return BadRequest("Para recarga, Origen != 0 y Destino debe ser null.");
            }
            else
            {
                return BadRequest();
            }

            // Obtener la o las cuentas de origen (solo si origen != 0)
            Cuenta cuentaOrigen = null!;
            if (dto.NroCuentaOrigen != 0)
            {
                // Verificamos que exista y que pertenezca al usuario
                cuentaOrigen = await _cuentaRepository.GetByIdWithUsuarioAsync(dto.NroCuentaOrigen);
                if (cuentaOrigen == null || cuentaOrigen.nro_cliente != nroCliente)
                    return Forbid("No tiene permiso sobre la cuenta de origen.");
            }

            // Obtener cuenta destino (solo si destino != null)
            Cuenta cuentaDestino = null!;
            if (dto.NroCuentaDestino.HasValue)
            {
                cuentaDestino = await _cuentaRepository.GetByIdWithUsuarioAsync(dto.NroCuentaDestino.Value);
                if (cuentaDestino == null)
                    return NotFound($"Cuenta destino {dto.NroCuentaDestino.Value} no existe.");
            }

            // Verificar saldo en origen (para retiro, transferencia, compra o recarga)
            if (codigoTransaccion == 3 || codigoTransaccion == 1 || codigoTransaccion == 4 || codigoTransaccion == 5)
            {
                var nroCtaOrig = dto.NroCuentaOrigen;
                var saldoOrigen = await _cuentaRepository.ObtenerSaldoAsync(nroCtaOrig);
                if (dto.Monto > saldoOrigen)
                    return BadRequest("Saldo insuficiente en cuenta de origen.");
            }

            //  Actualizar o crear registro en Transaccion con la descripción
            // **(Ya existe transac obtenido anteriormente; si queremos actualizar la descripción, la dejamos)**
            transac.descripcion = dto.Descripcion;             // **CAMBIO**: actualizamos la descripción si cambió
            await _transaccionRepository.SaveAsync();

            // Generar el próximo id_trx (debes tener GetMaxIdAsync en IMovimientoRepository)
            var nextId = await _movimientoRepository.GetMaxIdAsync() + 1;

            //  Crear la entidad Movimiento
            var mov = new Movimiento
            {
                id_trx = nextId,
                fecha = DateTime.UtcNow,
                monto = dto.Monto,
                nro_cuenta_orig = dto.NroCuentaOrigen,
                nro_cuenta_dest = dto.NroCuentaDestino,
               
            };

            await _movimientoRepository.CrearAsync(mov);
            await _movimientoRepository.SaveAsync();

            // Retornar Ok con mensaje
            return Ok(new { mensaje = "Movimiento procesado correctamente." });
        }









    }
}






        /*
            [HttpPost]
            public async Task<ActionResult<Cuenta>> PostCuenta(Cuenta cuenta)
            {
                await _cuentaRepository.AddAsync(cuenta);
                await _cuentaRepository.SaveAsync();

                return CreatedAtAction(nameof(GetCuenta), new { id = cuenta.nro_cuenta }, cuenta);
            }

            [HttpPut("{id}")]
            public async Task<IActionResult> PutCuenta(int id, Cuenta cuenta)
            {
                if (id != cuenta.nro_cuenta)
                    return BadRequest();

                _cuentaRepository.Update(cuenta);
                await _cuentaRepository.SaveAsync();

                return NoContent();
            }

            [HttpDelete("{id}")]
            public async Task<IActionResult> DeleteCuenta(int id)
            {
                var cuenta = await _cuentaRepository.GetByIdAsync(id);
                if (cuenta == null)
                    return NotFound();

                _cuentaRepository.Delete(cuenta);
                await _cuentaRepository.SaveAsync();

                return NoContent();
            }
        */
