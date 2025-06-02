using digitalArsv1.Models;
using Microsoft.EntityFrameworkCore;

namespace digitalArsv1
{
    public class DigitalArsContext : DbContext
    {
        public DigitalArsContext(DbContextOptions<DigitalArsContext> options) : base(options) { }

        public DbSet<Cuenta> Cuentas { get; set; }
        public DbSet<Movimiento> Movimientos { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Transaccion> Transacciones { get; set; }
        public DbSet<Permiso> Permisos { get; set; }//Agregado para la entidad permiso


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //  USUARIO 
            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.ToTable("Usuario");
                entity.HasKey(u => u.nro_cliente);
                entity.Property(u => u.nro_cliente).HasColumnName("nro_cliente");
                entity.Property(u => u.nombre).HasColumnName("nombre");
                entity.Property(u => u.apellido).HasColumnName("apellido");
                entity.Property(u => u.direccion).HasColumnName("direccion");
                entity.Property(u => u.mail).HasColumnName("mail");
                entity.Property(u => u.estado).HasColumnName("estado");
                entity.Property(u => u.telefono).HasColumnName("telefono");
            });

            //  CUENTA 
            modelBuilder.Entity<Cuenta>(entity =>
            {
                entity.ToTable("Cuenta");
                entity.HasKey(c => c.nro_cuenta);
                entity.Property(c => c.nro_cuenta) .HasColumnName("nro_cuenta")
                      .ValueGeneratedNever();

                entity.Property(c => c.producto)  .HasColumnName("producto");

                entity.Property(c => c.CBU)  .HasColumnName("CBU");

                entity.Property(c => c.estado)  .HasColumnName("estado");

                entity.Property(c => c.nro_cliente) .HasColumnName("nro_cliente");

                // Asegúrate de mapear `rol_cta` como nvarchar(max) (por convención no hace falta HasColumnType)
                entity.Property(c => c.rol_cta) .HasColumnName("rol_cta");

                // Especifica tipo decimal(18,2) para evitar truncamientos
                entity.Property(c => c.saldo) .HasColumnName("saldo")
                      .HasColumnType("decimal(18,2)");

                // `fecha_alta` ya es DateTime en la clase; aquí definimos el default y tipo
                entity.Property(c => c.fecha_alta) .HasColumnName("fecha_alta")
                      .HasColumnType("datetime2")
                      .HasDefaultValueSql("GETDATE()");

                entity.Property(c => c.alias) .HasColumnName("alias")
                      .HasMaxLength(50)
                      .IsRequired(false);

                entity.HasOne(c => c.Usuario)
                      .WithMany(u => u.Cuentas)
                      .HasForeignKey(c => c.nro_cliente)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            //  MOVIMIENTO 
            modelBuilder.Entity<Movimiento>(entity =>
            {
                entity.ToTable("Movimiento");
                entity.HasKey(m => m.id_trx);

                // Id_trx asignado manualmente
                entity.Property(m => m.id_trx)
                      .HasColumnName("id_trx")
                      .ValueGeneratedNever();

                entity.Property(m => m.fecha).HasColumnName("fecha");
                entity.Property(m => m.monto)
                      .HasColumnName("monto")
                      .HasColumnType("decimal(18,2)");
                entity.Property(m => m.nro_cuenta_orig).HasColumnName("nro_cuenta_orig");
                entity.Property(m => m.nro_cuenta_dest).HasColumnName("nro_cuenta_dest");
                entity.Property(m => m.codigo_transaccion).HasColumnName("codigo_transaccion");

                entity.HasOne(m => m.CuentaOrig)
                      .WithMany(c => c.MovimientosOrigen)
                      .HasForeignKey(m => m.nro_cuenta_orig)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(m => m.CuentaDest)
                      .WithMany(c => c.MovimientosDestino)
                      .HasForeignKey(m => m.nro_cuenta_dest)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(m => m.Transaccion)
                      .WithMany(t => t.Movimientos)
                      .HasForeignKey(m => m.codigo_transaccion)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // –––––– PERMISO ––––––
            modelBuilder.Entity<Permiso>(entity =>
            {
                entity.ToTable("Permisos");
                entity.HasKey(p => new { p.nro_usuario, p.acceso });
                entity.Property(p => p.nro_usuario).HasColumnName("nro_usuario");
                entity.Property(p => p.acceso).HasColumnName("acceso");

                entity.HasOne(p => p.Usuario)
                      .WithMany(u => u.Permisos)
                      .HasForeignKey(p => p.nro_usuario)
                      .OnDelete(DeleteBehavior.Cascade);
            });

        } 

    }  
}  