using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using VitaStudio.Api.Data;
using VitaStudio.Api.Dtos;
using VitaStudio.Api.Models;

namespace VitaStudio.Api.Services;

public interface IAuthService
{
    Task<AuthResponseDto?> LoginAsync(LoginDto dto);
    Task                   RegisterClanAsync(RegisterClanDto dto);
}

public class AuthService : IAuthService
{
    private readonly VitaStudioDbContext _db;
    private readonly IConfiguration _cfg;

    public AuthService(VitaStudioDbContext db, IConfiguration cfg)
    {
        _db  = db;
        _cfg = cfg;
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginDto dto)
    {
        switch (dto.Uloga.ToLowerInvariant())
        {
            case "clan":
            {
                var u = await _db.Clanovi.FirstOrDefaultAsync(c => c.Email == dto.Email);
                if (u is null || !BCrypt.Net.BCrypt.Verify(dto.Lozinka, u.Lozinka)) return null;
                if (u.Status == "pending")
                    throw new InvalidOperationException("pending");
                if (u.Status == "odbijen")
                    throw new InvalidOperationException("odbijen");
                return BuildResponse(u.Id, u.Ime, u.Prezime, u.Email, "clan");
            }
            case "trener":
            {
                var u = await _db.Treneri.FirstOrDefaultAsync(t => t.Email == dto.Email);
                if (u is null || !BCrypt.Net.BCrypt.Verify(dto.Lozinka, u.Lozinka)) return null;
                return BuildResponse(u.Id, u.Ime, u.Prezime, u.Email, "trener");
            }
            case "administrator":
            {
                var u = await _db.Administratori.FirstOrDefaultAsync(a => a.Email == dto.Email);
                    if (u is null || !BCrypt.Net.BCrypt.Verify(dto.Lozinka, u.Lozinka)) return null; ;
                    return BuildResponse(u.Id, u.Ime, u.Prezime, u.Email, "administrator");
            }
            default:
                return null;
        }
    }

    public async Task RegisterClanAsync(RegisterClanDto dto)
    {
        if (await _db.Clanovi.AnyAsync(c => c.Email == dto.Email))
            throw new InvalidOperationException("Email već postoji.");

        var clan = new Clan
        {
            Ime = dto.Ime, Prezime = dto.Prezime, Email = dto.Email,
            Lozinka = BCrypt.Net.BCrypt.HashPassword(dto.Lozinka, 11),
            Telefon = dto.Telefon, DatumRodjenja = dto.DatumRodjenja,
            Status = "pending"
        };
        _db.Clanovi.Add(clan);
        await _db.SaveChangesAsync();
    }

    private AuthResponseDto BuildResponse(int id, string ime, string prezime, string email, string uloga)
    {
        var keyStr = _cfg["Jwt:Key"] ?? throw new("Jwt:Key nije postavljen.");
        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyStr));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, id.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role,  uloga),
            new Claim("ime",     ime),
            new Claim("prezime", prezime)
        };

        var token = new JwtSecurityToken(
            issuer:   _cfg["Jwt:Issuer"],
            audience: _cfg["Jwt:Audience"],
            claims:   claims,
            expires:  DateTime.UtcNow.AddMinutes(int.Parse(_cfg["Jwt:ExpiresInMinutes"] ?? "480")),
            signingCredentials: creds
        );

        var tokenStr = new JwtSecurityTokenHandler().WriteToken(token);
        return new AuthResponseDto(tokenStr, id, ime, prezime, email, uloga);
    }
}
