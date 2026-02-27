using System.Security.Claims;
using System.Text;
using AgroIdentity.Api.Data;
using AgroIdentity.Api.Domain;
using AgroIdentity.Api.Dtos;
using AgroIdentity.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// ✅ Em produção/container, respeita PORT/WEBSITES_PORT (bom para Azure/Aks)
if (!builder.Environment.IsDevelopment())
{
    var port = Environment.GetEnvironmentVariable("WEBSITES_PORT")
              ?? Environment.GetEnvironmentVariable("PORT");

    if (!string.IsNullOrWhiteSpace(port))
        builder.WebHost.UseUrls($"http://*:{port}");
}

// 🔐 JWT configs (falha rápido se estiver faltando)
var jwtIssuer = builder.Configuration["Jwt:Issuer"]
               ?? throw new InvalidOperationException("Config Jwt:Issuer não encontrada.");
var jwtAudience = builder.Configuration["Jwt:Audience"]
                 ?? throw new InvalidOperationException("Config Jwt:Audience não encontrada.");
var jwtKey = builder.Configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Config Jwt:Key não encontrada.");

// 💾 Banco de Dados (Azure SQL) - DB único, schema identity e retry
builder.Services.AddDbContext<IdentityDbContext>(opt =>
    opt.UseSqlServer(
        builder.Configuration.GetConnectionString("Default")
        ?? throw new InvalidOperationException("ConnectionStrings:Default não encontrada."),
        sql => sql
            .MigrationsHistoryTable("__EFMigrationsHistory", "identity")
            .EnableRetryOnFailure(10, TimeSpan.FromSeconds(10), null)
    )
);

// Services
builder.Services.AddScoped<JwtTokenService>();

// 🔐 Auth/JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,

            ValidateAudience = true,
            ValidAudience = jwtAudience,

            ValidateLifetime = true,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),

            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

// 📄 Swagger com Authorize (Bearer) - compatível com seu stack atual
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "AgroIdentity API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header usando Bearer. Ex: \"Bearer {token}\""
    });

    // ✅ Forma que compila no seu projeto (sem OpenApiReference manual)
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

builder.Services.AddCors(o =>
{
    o.AddDefaultPolicy(p => p
        .AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader());
});

var app = builder.Build();

app.UseCors();

// 🌐 Middlewares
app.UseSwagger();
app.UseSwaggerUI();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

// Health
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// Register
app.MapPost("/auth/register", async (RegisterRequest req, IdentityDbContext db) =>
{
    var email = req.Email.Trim().ToLowerInvariant();

    if (string.IsNullOrWhiteSpace(req.Name))
        return Results.BadRequest(new { message = "Name é obrigatório." });

    if (string.IsNullOrWhiteSpace(req.Password) || req.Password.Length < 6)
        return Results.BadRequest(new { message = "Senha deve ter pelo menos 6 caracteres." });

    var exists = await db.Users.AnyAsync(u => u.Email == email);
    if (exists) return Results.Conflict(new { message = "Email já cadastrado." });

    var user = new User
    {
        Name = req.Name.Trim(),
        Email = email,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password)
    };

    db.Users.Add(user);
    await db.SaveChangesAsync();

    return Results.Created($"/users/{user.Id}", new { user.Id, user.Name, user.Email });
});

// Login
app.MapPost("/auth/login", async (LoginRequest req, IdentityDbContext db, JwtTokenService jwt) =>
{
    var email = req.Email.Trim().ToLowerInvariant();
    var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);

    if (user is null) return Results.Unauthorized();
    if (!BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash)) return Results.Unauthorized();

    var (token, expiresAt) = jwt.Generate(user);
    return Results.Ok(new AuthResponse(token, expiresAt));
});

// Me
app.MapGet("/me", [Authorize] (ClaimsPrincipal principal) =>
{
    var idStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
    var name = principal.FindFirstValue(ClaimTypes.Name) ?? "";
    var email = principal.FindFirstValue(ClaimTypes.Email) ?? "";

    if (idStr is null || !Guid.TryParse(idStr, out var id))
        return Results.Unauthorized();

    return Results.Ok(new MeResponse(id, name, email));
});

app.Run();