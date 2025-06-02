using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using digitalArsv1;
using digitalArsv1.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection; // necesario para AddDbContext, AddScoped, etc.

var builder = WebApplication.CreateBuilder(args);

// ─── Configuración de Swagger/OpenAPI ───────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "BILLETERA VIRTUAL - DigitalArs",
        Version = "v1",
        Description = "Gestión de usuarios, cuentas, movimientos, permisos"
    });

    // Configuración de seguridad para que Swagger pueda enviar el Bearer token
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingrese 'Bearer {token}'"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] { }
        }
    });
});

// ─── Configuración de DbContext ──────────────────────────────────────────────────────
// Asegúrate de que en appsettings.json exista ConnectionStrings: { "DigitalArsConnection": "..." }
builder.Services.AddDbContext<DigitalArsContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DigitalArsConnection")));

// ─── Configuración de serialización JSON ─────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(x =>
        x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve);

// ─── Registro de repositorios en DI ─────────────────────────────────────────────────
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<ICuentaRepository, CuentaRepository>();
builder.Services.AddScoped<IMovimientoRepository, MovimientoRepository>();
builder.Services.AddScoped<ITransaccionRepository, TransaccionRepository>();
builder.Services.AddScoped<IPermisoRepository, PermisoRepository>();

// ─── CORS (para permitir llamadas desde Swagger u otros orígenes) ──────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSwagger", policy =>
    {
        policy
            .AllowAnyOrigin()   // Permitir cualquier origen para pruebas (ajusta según tu política real)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// ─── Configuración de JWT Authentication ─────────────────────────────────────────────
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var issuer = builder.Configuration["Jwt:Issuer"];
        var audience = builder.Configuration["Jwt:Audience"];
        var secret = builder.Configuration["Jwt:Key"];

        if (string.IsNullOrWhiteSpace(secret))
            throw new InvalidOperationException("La configuración 'Jwt:Key' no está definida.");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(secret))
        };
    });

var app = builder.Build();

// ─── Middleware pipeline ──────────────────────────────────────────────────────────────

// 1) CORS antes de todo lo demás
app.UseCors("AllowSwagger");

// 2) Swagger siempre disponible (no solo en Development)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "BILLETERA VIRTUAL - DigitalArs v1");
    c.RoutePrefix = "swagger"; // El UI estará en /swagger/index.html
});

// 3) HTTPS redirection
app.UseHttpsRedirection();

// 4) Autenticación y autorización
app.UseAuthentication();
app.UseAuthorization();

// 5) Mapear controladores
app.MapControllers();

app.Run();
