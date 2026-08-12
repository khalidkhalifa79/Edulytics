using System.ComponentModel.DataAnnotations;

namespace Edulytics.Web.ViewModels;

public sealed class LoginViewModel
{
    [Required(ErrorMessage = "EmailRequired")]
    [EmailAddress(ErrorMessage = "EmailInvalid")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "PasswordRequired")]
    [DataType(DataType.Password)]
    public string? Password { get; set; }
}
