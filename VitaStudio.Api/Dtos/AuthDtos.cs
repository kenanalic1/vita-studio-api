namespace VitaStudio.Api.Dtos;

public record LoginDto(string Email, string Lozinka, string Uloga);

public record AuthResponseDto(
    string Token,
    int Id,
    string Ime,
    string Prezime,
    string Email,
    string Uloga
);

public record RegisterClanDto(
    string Ime,
    string Prezime,
    string Email,
    string Lozinka,
    string Telefon,
    DateTime DatumRodjenja
);