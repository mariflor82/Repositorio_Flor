using digitalArsv1.DTOs;
using digitalArsv1.Helpers;
using digitalArsv1.Models;
using digitalArsv1.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace digitalArsv1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IPermisoRepository _permisoRepository;
        private readonly IConfiguration _configuration;

        public AuthController(
            IUsuarioRepository usuarioRepository,
            IPermisoRepository permisoRepository,
            IConfiguration configuration)
        {
            _usuarioRepository = usuarioRepository;
            _permisoRepository = permisoRepository;
            _configuration = configuration;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO login)
        {
            var usuario = await _usuarioRepository.ObtenerPorEmailAsync(login.Mail);
            if (usuario == null || !PasswordHelper.VerifyPassword(login.Password, usuario.password_hash))
                return Unauthorized(new { mensaje = "Credenciales inválidas." });

            if (!usuario.estado)
                return Unauthorized(new { mensaje = "El usuario está desactivado." });

            var token = GenerarToken(usuario.nro_cliente, usuario.tipo_cliente);

            return Ok(new
            {
                token,
                usuario = new UsuarioDTO
                {
                    Id = usuario.nro_cliente,
                    mail = usuario.mail,
                    Nombre = usuario.nombre,
                    tipo_cliente = usuario.tipo_cliente
                }
            });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            // 1) Validar que no exista duplicado
            var existente = await _usuarioRepository.ObtenerPorEmailAsync(request.Mail);
            if (existente != null)
                return BadRequest(new { mensaje = "Ya existe un usuario con ese mail." });

            // 2) Mapear DTO -> Entidad y guardar
            var usuario = new Usuario
            {
                nombre = request.Nombre,
                apellido = request.Apellido,
                mail = request.Mail,
                direccion = request.Direccion,
                telefono = request.Telefono,
                password_hash = PasswordHelper.HashPassword(request.Password),
                tipo_cliente = request.Tipo_cliente,
                estado = true
            };

            await _usuarioRepository.CrearAsync(usuario);
            await _usuarioRepository.SaveAsync(); // ← ¡Importante hacer el SaveAsync!

            return Ok(new { mensaje = "Usuario registrado correctamente." });
        }

        // Método privado para generar JWT
        private string GenerarToken(int nroUsuario, string tipoCliente)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, nroUsuario.ToString()),
                new Claim(ClaimTypes.Role, tipoCliente)
            };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(2),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [Authorize(Roles = "ADMINISTRADOR")]
        [HttpPut("desactivar-usuarios-sin-cuenta")]
        public async Task<IActionResult> DesactivarUsuariosSinCuenta()
        {
            var usuariosSinCuenta = await _usuarioRepository.ObtenerUsuariosSinCuentaAsync();

            if (!usuariosSinCuenta.Any())
                return NotFound("No hay usuarios sin cuentas para desactivar.");

            await _usuarioRepository.DesactivarUsuariosAsync(usuariosSinCuenta);

            return Ok(new
            {
                mensaje = $"{usuariosSinCuenta.Count} usuario(s) desactivados exitosamente.",
                usuariosDesactivados = usuariosSinCuenta.Select(u => new
                {
                    u.nro_cliente,
                    u.nombre,
                    u.apellido,
                    u.mail
                })
            });
        }
    }
}

