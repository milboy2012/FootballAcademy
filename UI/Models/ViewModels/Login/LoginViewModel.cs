using System.ComponentModel.DataAnnotations;

namespace UI.Models.ViewModels.Login
{
    public class LoginViewModel
    {
        [Required]
        //[EmailAddress]
        [Display(Name = "Email(Login)")]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Пароль")]
        public string Password { get; set; }

        [Display(Name = "Оставаться в сети")]        
        public bool RememberMe { get; set; }
    }
}
