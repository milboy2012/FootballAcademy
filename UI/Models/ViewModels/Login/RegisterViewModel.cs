using System.ComponentModel.DataAnnotations;

namespace UI.Models.ViewModels.Login
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Email")]
        [EmailAddress(ErrorMessage = "Неправильный email")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Поле Пароль не может быть пустым")]
        [DataType(DataType.Password)]
        [StringLength(100, ErrorMessage = "Пароль должен иметь минимум {2} символ(ов)", MinimumLength = 6)]
        [Display(Name = "Пароль")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Подтвердите пароль")]
        [Compare("Password", ErrorMessage = "Пароль должен совпадать")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Поле Имя обязательно для заполнения")]
        [Display(Name = "Имя")]
        [StringLength(50, MinimumLength = 2)]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Поле Фамилия обязательно для заполнения")]
        [Display(Name = "Фамилия")]
        [StringLength(50, MinimumLength = 2)]
        public string LastName { get; set; }

        [Display(Name = "Роль")]
        public string SelectedRole { get; set; }

        public List<RoleViewModel> Roles { get; set; } = new List<RoleViewModel>();
    }
}
