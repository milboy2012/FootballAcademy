using System.ComponentModel.DataAnnotations;

namespace UI.Models.ViewModels.Login
{
    public class ChangePasswordViewModel
    {
        [Required] 
        public string CurrentPassword { get; set; } = null!;
        [Required, MinLength(6, ErrorMessage = "Минимум 6 символов")] 
        public string NewPassword { get; set; } = null!;
        [Compare(nameof(NewPassword), ErrorMessage = "Пароли не совпадают")] 
        public string ConfirmPassword { get; set; } = null!;
    }
}
