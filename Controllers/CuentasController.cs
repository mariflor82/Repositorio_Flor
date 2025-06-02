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
using static digitalArsv1.DTOs.CuentaConsultaDTO;


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
        private static readonly string[] _aliasWords = new[] //como mejora se pondria una libreria de nombres
                    {
                        "sol", "luna", "estrella", "cielo", "mar", "rio", "bosque", "montaña",
                        "viento", "fuego", "tierra", "roca", "hoja", "flor", "hojas", "arena",
                        "brisa", "nube", "valle", "lago", "piedra", "sendero", "garza", "palma"
                    };

    public CuentasController(
            ICuentaRepository cuentaRepository,
            IMovimientoRepository movimientoRepository,
            ITransaccionRepository transaccionRepository)
        {
            _cuentaRepository = cuentaRepository;
            _movimientoRepository = movimientoRepository;
            _transaccionRepository = transaccionRepository;
        }
        // ================================
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Cuenta>>> GetCuentas()
        {
            var cuentas = await _cuentaRepository.GetAllWithUsuarioAsync();
            return Ok(cuentas);
        }
        // ================================
        

        // ← Aquí cambió la ruta para evitar ambigüedad con "{id}"
        [HttpGet("saldo/{nroCuenta}")]
        public async Task<ActionResult<decimal>> ObtenerSaldo(int nroCuenta)
        {
            var saldo = await _cuentaRepository.ObtenerSaldoAsync(nroCuenta);
            return Ok(saldo);
        }

        // ================================
        [HttpGet("{id}")]
        public async Task<ActionResult<Cuenta>> GetCuenta(int id)
        {
            var cuenta = await _cuentaRepository.GetByIdWithUsuarioAsync(id);
            if (cuenta == null)
                return NotFound();

            return Ok(cuenta);
        }

        // ================================
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
            //Generar ALIAS aleatorio(4 palabras separadas por puntos) * *
            var rnd = new Random();
            var palabrasAlias = new List<string>();
            for (int i = 0; i < 4; i++)
            {
                // escogemos un índice aleatorio del arreglo _aliasWords
                var idx = rnd.Next(0, _aliasWords.Length);
                palabrasAlias.Add(_aliasWords[idx]);
            }
            var aliasFinal = string.Join('.', palabrasAlias);


            // El número de cuenta son los últimos 5 dígitos del CBU
            var ultimosCinco = cbu.Substring(cbu.Length - 5);       // Extraer últimos 5 dígitos
            var nroCuentaInt = int.Parse(ultimosCinco);
            var nuevaCuenta = new Cuenta

            {
                producto = "Caja de ahorro",
                CBU = cbu,
                estado = true,
                nro_cliente = nroCliente,
                rol_cta = "Titular",
                nro_cuenta = nroCuentaInt,
                alias = aliasFinal
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
                RolCta = nuevaCuenta.rol_cta,
                FechaCreacion = nuevaCuenta.fecha_alta,
                Alias=nuevaCuenta.alias
            };

            return CreatedAtAction(
                nameof(GetCuenta),
                new { id = result.NroCuenta },
                result);
        }
        // ================================
        // GET /api/Cuentas/por-cliente
        /// Devuelve solo NroCuenta, CBU, Nombre y Apellido del Usuario, y Saldo.
        
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
        // ================================
        //Modificar ALIAS de la cuenta

        [Authorize(Roles = "Billetera")]
        [HttpPut("alias")]
        public async Task<ActionResult> ActualizarAlias([FromBody] UpdateAliasDTO dto)
        {
            // 1️⃣ Validar que el DTO no sea null
            if (dto == null)
                return BadRequest("El cuerpo de la petición no puede estar vacío.");

            if (string.IsNullOrWhiteSpace(dto.NuevoAlias))
                return BadRequest("El nuevo alias no puede estar vacío.");

            // 2️⃣ Extraer nro_cliente del JWT para verificar pertenencia
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? User.FindFirstValue("sub");
            if (!int.TryParse(userId, out var nroCliente))
                return Forbid("Token inválido.");

            // 3️⃣ Obtener la cuenta solicitada
            var cuenta = await _cuentaRepository.GetByIdWithUsuarioAsync(dto.NroCuenta);
            if (cuenta == null)
                return NotFound($"La cuenta {dto.NroCuenta} no existe.");

            // 4️⃣ Verificar que la cuenta pertenezca al usuario autenticado
            if (cuenta.nro_cliente != nroCliente)
                return Forbid("No tienes permiso para modificar esta cuenta.");

            // 5️⃣ Actualizar el campo alias de la entidad
            cuenta.alias = dto.NuevoAlias; // **CAMBIO: guardamos el nuevo alias**

            // 6️⃣ Persistir en BD
            _cuentaRepository.Update(cuenta);
            await _cuentaRepository.SaveAsync();

            // 7️⃣ Devolver mensaje de confirmación
            return Ok($"Alias actualizado correctamente.");
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
