using System.ComponentModel.DataAnnotations;

namespace AgroIdentity.Api.Domain;

public class User
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(120)]
    public string Name { get; set; } = default!;

    [Required, MaxLength(180)]
    public string Email { get; set; } = default!;

    [Required]
    public string PasswordHash { get; set; } = default!;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}