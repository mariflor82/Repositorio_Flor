namespace digitalArsv1.DTOs
{
    public class RegisterRequest
    {
        public string Mail { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string Password { get; set; }

        public string Direccion { get; set; }

        public string Telefono{ get; set; }
        public string Tipo_cliente { get; set; }

       // public List<CuentaDTO> Cuentas { get; set; } = new(); //para la lista de cuentas del usuario
    }

    public class LoginDTO
    {
        public string Mail { get; set; }
        public string Password { get; set; }
    }

    public class UsuarioDTO
    {
        public int Id { get; set; }
        public string mail { get; set; }
        public string Nombre { get; set; }
        public string tipo_cliente { get; set; }
        // ✅ NUEVO: incluir lista de cuentas del usuario
        
    }
    // DTO PARA ACTUALIZAR USUARIO**
    // ============================
    public class UpdateUsuarioDTO
    {
        /// Nombre completo (opcional para actualizar).
        public string? Nombre { get; set; }
             
        /// Apellido (opcional para actualizar).
        public string? Apellido { get; set; }
               
        /// Dirección (opcional para actualizar).
        public string? Direccion { get; set; }
                
        /// Mail (opcional para actualizar).
       public string? Mail { get; set; }
               
        /// Teléfono (opcional para actualizar).
        public string? Telefono { get; set; }
    }
}