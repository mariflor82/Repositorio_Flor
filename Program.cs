using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using digitalArsv1;
using digitalArsv1.Repositories;
using Microsoft.Extensions.Configuration; //  esto si usas IConfiguration en controladores





var builder = WebApplication.CreateBuilder(args);

// explorador de endpoints y Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "BILLETERA VIRTUAL- DigitalArs",
        Version = "v1",
        Description = "Gestión de usuarios, cuentas, movimientos, permisos " });

// Configuración de seguridad para JWT
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingrese el token "
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
            new string[] {}
        }
    });
});

// Configura el DbContext
builder.Services.AddDbContext<DigitalArsContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DigitalArsConnection")));

// Configura la serialización JSON
builder.Services.AddControllers()
    .AddJsonOptions(x =>
        x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve);

// Registro de repositorios
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<ICuentaRepository, CuentaRepository>();
builder.Services.AddScoped<IMovimientoRepository, MovimientoRepository>();
builder.Services.AddScoped<ITransaccionRepository, TransaccionRepository>();
builder.Services.AddScoped<IPermisoRepository, PermisoRepository>();

//Permitir CORS desde Swagger (https://localhost:7153) **
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSwagger", policy =>
    {
        policy
            .AllowAnyOrigin()    // Permitir *cualquier* origen
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});









// **Configuración de Autenticación JWT:**
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // 1) Leer valores del appsettings.json
        var issuer = builder.Configuration["Jwt:Issuer"];
        var audience = builder.Configuration["Jwt:Audience"];
        var secret = builder.Configuration["Jwt:Key"]; 

        // 2) Validar que NO SEA null o vacío
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
                Encoding.UTF8.GetBytes(secret))   // ? Aquí 'secret' nunca será null
        };
    });
var app = builder.Build();

// Middleware pipeline

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// **───────────────────────────────────────────────────────────────**
// ** CAMBIO: Usar la política CORS “AllowSwagger” antes de UseAuthentication **
app.UseCors("AllowSwagger");
// **───────────────────────────────────────────────────────────────**

app.UseAuthentication();  // <- Esto debe ir antes que UseAuthorization
app.UseAuthorization();

app.MapControllers();

app.Run();