using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace digitalArsv1.Migrations
{
    /// <inheritdoc />
    public partial class Back : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Transacciones",
                columns: table => new
                {
                    codigo_transaccion = table.Column<int>(type: "int", nullable: false),
                    descripcion = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transacciones", x => x.codigo_transaccion);
                });

            migrationBuilder.CreateTable(
                name: "Usuario",
                columns: table => new
                {
                    nro_cliente = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    apellido = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    direccion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    mail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    estado = table.Column<bool>(type: "bit", nullable: false),
                    tipo_cliente = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    telefono = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    password_hash = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuario", x => x.nro_cliente);
                });

            migrationBuilder.CreateTable(
                name: "Cuenta",
                columns: table => new
                {
                    nro_cuenta = table.Column<int>(type: "int", nullable: false),
                    producto = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CBU = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    estado = table.Column<bool>(type: "bit", nullable: false),
                    nro_cliente = table.Column<int>(type: "int", nullable: false),
                    rol_cta = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    saldo = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    fecha_alta = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    alias = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cuenta", x => x.nro_cuenta);
                    table.ForeignKey(
                        name: "FK_Cuenta_Usuario_nro_cliente",
                        column: x => x.nro_cliente,
                        principalTable: "Usuario",
                        principalColumn: "nro_cliente",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Permisos",
                columns: table => new
                {
                    nro_usuario = table.Column<int>(type: "int", nullable: false),
                    acceso = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permisos", x => new { x.nro_usuario, x.acceso });
                    table.ForeignKey(
                        name: "FK_Permisos_Usuario_nro_usuario",
                        column: x => x.nro_usuario,
                        principalTable: "Usuario",
                        principalColumn: "nro_cliente",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Movimiento",
                columns: table => new
                {
                    id_trx = table.Column<int>(type: "int", nullable: false),
                    fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    monto = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    nro_cuenta_orig = table.Column<int>(type: "int", nullable: true),
                    nro_cuenta_dest = table.Column<int>(type: "int", nullable: true),
                    codigo_transaccion = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Movimiento", x => x.id_trx);
                    table.ForeignKey(
                        name: "FK_Movimiento_Cuenta_nro_cuenta_dest",
                        column: x => x.nro_cuenta_dest,
                        principalTable: "Cuenta",
                        principalColumn: "nro_cuenta",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Movimiento_Cuenta_nro_cuenta_orig",
                        column: x => x.nro_cuenta_orig,
                        principalTable: "Cuenta",
                        principalColumn: "nro_cuenta",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Movimiento_Transacciones_codigo_transaccion",
                        column: x => x.codigo_transaccion,
                        principalTable: "Transacciones",
                        principalColumn: "codigo_transaccion",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cuenta_nro_cliente",
                table: "Cuenta",
                column: "nro_cliente");

            migrationBuilder.CreateIndex(
                name: "IX_Movimiento_codigo_transaccion",
                table: "Movimiento",
                column: "codigo_transaccion");

            migrationBuilder.CreateIndex(
                name: "IX_Movimiento_nro_cuenta_dest",
                table: "Movimiento",
                column: "nro_cuenta_dest");

            migrationBuilder.CreateIndex(
                name: "IX_Movimiento_nro_cuenta_orig",
                table: "Movimiento",
                column: "nro_cuenta_orig");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Movimiento");

            migrationBuilder.DropTable(
                name: "Permisos");

            migrationBuilder.DropTable(
                name: "Cuenta");

            migrationBuilder.DropTable(
                name: "Transacciones");

            migrationBuilder.DropTable(
                name: "Usuario");
        }
    }
}
