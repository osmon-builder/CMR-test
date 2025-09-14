using Microsoft.EntityFrameworkCore;
using MiniCrm.Api.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

// --- builder ---
var builder = WebApplication.CreateBuilder(args);

// DB
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Default"))
//.UseSnakeCaseNamingConvention() // opcional (regenera migraciones si activas)
);

// CORS
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("spa", p => p
        .WithOrigins("http://localhost:4200")
        .AllowAnyHeader()
        .AllowAnyMethod());
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Tenant + HttpContext
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantProvider, TenantProvider>();

// JWT
var jwt = builder.Configuration.GetSection("Jwt");
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = key
        };
    });
builder.Services.AddAuthorization();

// --- app ---
var app = builder.Build();

app.UseCors("spa");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Ping
app.MapGet("/", () => Results.Ok(new { ok = true, env = app.Environment.EnvironmentName }));

// ---------- AUTH ----------
app.MapPost("/auth/register-tenant", async (AppDbContext db, IConfiguration cfg, RegisterTenantDto dto) =>
{
    if (string.IsNullOrWhiteSpace(dto.TenantName) ||
        string.IsNullOrWhiteSpace(dto.AdminEmail) ||
        string.IsNullOrWhiteSpace(dto.Password))
        return Results.BadRequest(new { message = "All fields are required." });

    var tenant = new Tenant { Name = dto.TenantName };
    db.Add(tenant);

    var hash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
    var user = new User { TenantId = tenant.Id, Email = dto.AdminEmail, PasswordHash = hash };
    db.Add(user);

    await db.SaveChangesAsync();

    var token = JwtHelper.CreateToken(user.Id, tenant.Id, user.Email, cfg, key);
    return Results.Ok(new TokenResponse(token));
});

app.MapPost("/auth/login", async (AppDbContext db, IConfiguration cfg, LoginDto dto) =>
{
    var user = await db.Users.FirstOrDefaultAsync(x => x.Email == dto.Email);
    if (user is null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        return Results.Unauthorized();

    var token = JwtHelper.CreateToken(user.Id, user.TenantId, user.Email, cfg, key);
    return Results.Ok(new TokenResponse(token));
});

// ---------- CLIENTS (todo protegido) ----------
var clients = app.MapGroup("/clients").RequireAuthorization();

// GET /clients?search=&page=1&pageSize=10
clients.MapGet("/", async (AppDbContext db, string? search, int page = 1, int pageSize = 10) =>
{
    if (page < 1) page = 1;
    if (pageSize is < 1 or > 100) pageSize = 10;

    var q = db.Clients.AsQueryable();

    if (!string.IsNullOrWhiteSpace(search))
        q = q.Where(c => c.Name.Contains(search) || c.Email.Contains(search));

    var total = await q.CountAsync();
    var items = await q.OrderByDescending(c => c.CreatedAt)
                       .Skip((page - 1) * pageSize)
                       .Take(pageSize)
                       .ToListAsync();

    return Results.Ok(new { total, page, pageSize, items });
});

// POST /clients
clients.MapPost("/", async (AppDbContext db, ITenantProvider tenant, Client dto) =>
{
    var c = new Client { Name = dto.Name, Email = dto.Email, Phone = dto.Phone, TenantId = tenant.TenantId };
    db.Add(c);
    await db.SaveChangesAsync();
    return Results.Created($"/clients/{c.Id}", c);
});

// PUT /clients/{id}
clients.MapPut("/{id:guid}", async (AppDbContext db, Guid id, Client dto) =>
{
    var c = await db.Clients.FindAsync(id);
    if (c is null) return Results.NotFound();
    c.Name = dto.Name; c.Email = dto.Email; c.Phone = dto.Phone;
    await db.SaveChangesAsync();
    return Results.NoContent();
});

// DELETE /clients/{id}
clients.MapDelete("/{id:guid}", async (AppDbContext db, Guid id) =>
{
    var c = await db.Clients.FindAsync(id);
    if (c is null) return Results.NotFound();
    db.Remove(c);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.Run();
