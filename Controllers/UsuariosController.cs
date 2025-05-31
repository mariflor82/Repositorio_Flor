using digitalArsv1.DTOs;
using digitalArsv1.Models;
using digitalArsv1.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Linq;                 // ← NECESARIO para .Select(...) en dtocUENTAS
using Microsoft.Extensions.Configuration; // ✅ NUEVO: para poder usar IConfiguration




[ApiController]
[Route("api/[controller]")]
public class UsuariosController : ControllerBase
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly ICuentaRepository _cuentaRepository;
    private readonly IConfiguration _config;

    public UsuariosController(
        IUsuarioRepository usuarioRepository,
        ICuentaRepository cuentaRepository,   // ✅ NUEVO: inyectar también ICuentaRepository
        IConfiguration config)

    {
        _usuarioRepository = usuarioRepository;
        _cuentaRepository = cuentaRepository; // ✅ NUEVO: asignar la dependencia inyectada
        _config = config;
    }

    [HttpGet] // muestra usuarios que puede ver el admin
    public async Task<ActionResult<IEnumerable<UsuarioDTO>>> GetUsuarios()
    {
        var usuarios = await _usuarioRepository.GetAllAsync();
        var usuariosDto = usuarios.Select(u => new UsuarioDTO
        {
            Id = u.nro_cliente,
            mail = u.mail,
            Nombre = u.nombre + " " + u.apellido,  // Para concatenar nombre + apellido,
            tipo_cliente = u.tipo_cliente
        });

        return Ok(usuariosDto);
    }

    [HttpGet("{id}")] //levanta datos del usuario id
    public async Task<ActionResult<UsuarioDTO>> GetUsuario(int id)
    {
        var usuario = await _usuarioRepository.GetByIdAsync(id);
        if (usuario == null)
            return NotFound();

        var usuarioDto = new UsuarioDTO
        {
            Id = usuario.nro_cliente,
            mail = usuario.mail,
            Nombre = usuario.nombre,
            tipo_cliente = usuario.tipo_cliente
        };

        return Ok(usuarioDto);
    }


 [HttpPost] // METODO POST - crea usuario de app
    public async Task<ActionResult<UsuarioDTO>> PostUsuario([FromBody] RegisterRequest dto)
    {
        // 1️⃣ Validar datos obligatorios
            if (string.IsNullOrWhiteSpace(dto.Mail))
                return BadRequest("El correo (Mail) es obligatorio.");
            if (string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest("La contraseña (Password) es obligatoria.");
            if (string.IsNullOrWhiteSpace(dto.Nombre)
               || string.IsNullOrWhiteSpace(dto.Apellido))
                return BadRequest("Nombre y Apellido son obligatorios.");
            if (string.IsNullOrWhiteSpace(dto.Tipo_cliente))
                return BadRequest("El tipo de cliente es obligatorio.");

       // 2️⃣ Verificar si ya existe un usuario con ese correo
             var existente = await _usuarioRepository.ObtenerPorEmailAsync(dto.Mail);
             if (existente != null)
                return Conflict("Ya existe un usuario registrado con ese correo.");

        // 3️⃣ Mapear DTO -> Entidad Usuario y generar hash BCrypt
        var usuario = new Usuario
        {
            mail = dto.Mail,
            nombre = dto.Nombre,
            apellido = dto.Apellido,
            direccion = dto.Direccion,
            telefono = dto.Telefono,
            tipo_cliente = dto.Tipo_cliente,
            // Aquí generamos el HASH (por ejemplo: "$2a$10$abcd...") en NVARCHAR(MAX)
            password_hash = BCrypt.Net.BCrypt.HashPassword(dto.Password) // ✅ NUEVO
        };
      

         // 4️⃣ Guardar en la base de datos
                 await _usuarioRepository.CrearAsync(usuario);
                 await _usuarioRepository.SaveAsync();

        // 5️⃣ Preparar DTO de respuesta (no expongo el hash)
        var usuarioDto = new UsuarioDTO
        {
            Id = usuario.nro_cliente,
            mail = usuario.mail,
            Nombre = usuario.nombre + " " + usuario.apellido,
            tipo_cliente = usuario.tipo_cliente
        };

        return CreatedAtAction(nameof(GetUsuario),
        new { id = usuario.nro_cliente },
        usuarioDto);
    }

    [HttpPost("login")] // login del usuario creado anteriormente
    public async Task<ActionResult<string>> Login([FromBody] LoginDTO dto)
    {
        // 1️⃣ Buscar el usuario por email
       var usuario = await _usuarioRepository.ObtenerPorEmailAsync(dto.Mail);
            if (usuario == null)
            return Unauthorized("Credenciales inválidas.");  // ✅ NUEVO: separar existencia de usuario

        // 2️⃣ Validar que el campo `password_hash` tenga formato BCrypt estándar
        //    (debe empezar con "$2a$", "$2b$" o "$2y$")
        if (string.IsNullOrWhiteSpace(usuario.password_hash) ||
              !(usuario.password_hash.StartsWith("$2a$") ||
                usuario.password_hash.StartsWith("$2b$") ||
                usuario.password_hash.StartsWith("$2y$")))
        {
            // Si el valor en BD NO es un hash BCrypt válido, devolvemos 401
            return Unauthorized("Credenciales inválidas.");            // ✅ NUEVO
        }

        // 3️⃣ Verificar la contraseña con try/catch para atrapar SaltParseException
        try
        { 
            bool esValida = BCrypt.Net.BCrypt.Verify(dto.Password, usuario.password_hash); // ⚠️ AJUSTE
            if (!esValida)
                return Unauthorized("Credenciales inválidas.");                        // ✅ NUEVO
        }
        
        catch (BCrypt.Net.SaltParseException)
       {
            // Si el hash en BD está malformado, devolvemos 401 sin exponer detalles
            return Unauthorized("Credenciales inválidas.");                            // ✅ NUEVO
        }


        // 4️⃣ Crear claims y token JWT
        var claims = new[]
        {
                new Claim(ClaimTypes.NameIdentifier, usuario.nro_cliente.ToString()),
                new Claim(ClaimTypes.Email, usuario.mail),
                new Claim(ClaimTypes.Role, "Billetera") // o el rol real que corresponda
            };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: creds
        );

        return Ok(new JwtSecurityTokenHandler().WriteToken(token));
        }

      private bool VerifyPassword(string plain, string storedHash)
       {
           // Aquí haces tu lógica de verificación de hash,
           // por ejemplo con BCrypt.Net-Next, etc.
           return BCrypt.Net.BCrypt.Verify(plain, storedHash);
       }
       // ⚠️ NOTA: Ya no necesitamos un método separado VerifyPassword,
       //          usamos directamente BCrypt.Net.BCrypt.Verify(...) en el login.
    }

















/*


    [HttpPost]//METODO POST - crea usuario de app
    public async Task<ActionResult<Usuario>> PostUsuario(Usuario usuario)
    {
        await _usuarioRepository.CrearAsync(usuario);
        await _usuarioRepository.SaveAsync();
        //agrego flor
        var usuarioDto = new UsuarioDTO
        {
            Id = usuario.nro_cliente,
            mail = usuario.mail,
            Nombre = usuario.nombre,
            tipo_cliente = usuario.tipo_cliente

        };

        return CreatedAtAction(nameof(GetUsuario), new { id = usuario.nro_cliente }, usuario);

    }
    [HttpPost("login")]//login del usuario creado anteriormente
    public async Task<ActionResult<string>> Login([FromBody] LoginDTO dto)
    {
        // 1. Validar existencia y contraseña
        var usuario = await _usuarioRepository.ObtenerPorEmailAsync(dto.Mail);
        if (usuario == null || !VerifyPassword(dto.Password, usuario.password_hash))
            return Unauthorized("Credenciales inválidas");

  // ⚠️ AJUSTE: Capturar posible excepción si `usuario.password_hash` no es un salt válido
        try
        {
            if (!VerifyPassword(dto.Password, usuario.password_hash))
                return Unauthorized("Credenciales inválidas");
        }
        catch (BCrypt.Net.SaltParseException)
        {
            // El hash en BD no tiene la forma esperada; rechazo de login
            return Unauthorized("Error de autenticación: formato de contraseña inválido");
        }
;        // 2. Validar contraseña con try/catch para atrapar SaltParseException
        try
        {
            if (!BCrypt.Net.BCrypt.Verify(dto.Password, usuario.password_hash))
                return Unauthorized("Credenciales inválidas");
        }
        catch (BCrypt.Net.SaltParseException)
        {
            // Si el salt de la base de datos no es válido, devolvemos 401 en lugar de excepción
            return Unauthorized("Credenciales inválidas");
        }

        // 2. Crear claims VER
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.nro_cliente.ToString()),
            new Claim(ClaimTypes.Email, usuario.mail!),
            new Claim(ClaimTypes.Role, "Billetera") // o el rol real que corresponda
        };

        // 3. Generar token
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Secret"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: creds
        );

        return Ok(new JwtSecurityTokenHandler().WriteToken(token));
    }
    private bool VerifyPassword(string plain, string storedHash)
    {
        // Aquí haces tu lógica de verificación de hash,
        // por ejemplo con BCrypt.Net-Next, etc.
        return BCrypt.Net.BCrypt.Verify(plain, storedHash);


        /* [HttpPut("{id}")]
         public async Task<IActionResult> PutUsuario(int id, Usuario usuario)
         {
             if (id != usuario.nro_cliente)
                 return BadRequest();

             _usuarioRepository.Update(usuario);
             await _usuarioRepository.SaveAsync();

             return NoContent();
         }

         [HttpDelete("{id}")]
         public async Task<IActionResult> DeleteUsuario(int id)
         {
             var usuario = await _usuarioRepository.GetByIdAsync(id);
             if (usuario == null)
                 return NotFound();

             _usuarioRepository.Delete(usuario);
             await _usuarioRepository.SaveAsync();

             return NoContent();
         }
         */
    