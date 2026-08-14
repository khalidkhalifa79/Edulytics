using System.ComponentModel.DataAnnotations;

namespace Edulytics.Web.ViewModels;

public sealed class SetPasswordViewModel
{
    public Guid UserId { get; set; }

    public string Token { get; set; } =
        string.Empty;

    public string Culture { get; set; } =
        "en";

    [Required(ErrorMessage = "PasswordRequired")]
    [DataType(DataType.Password)]
    public string Password { get; set; } =
        string.Empty;

    [Required(ErrorMessage = "ConfirmPasswordRequired")]
    [DataType(DataType.Password)]
    [Compare(
        nameof(Password),
        ErrorMessage = "PasswordsDoNotMatch")]
    public string ConfirmPassword { get; set; } =
        string.Empty;
}
