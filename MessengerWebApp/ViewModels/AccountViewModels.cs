using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace MessengerWebApp.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "{0} is required.")]
        [Display(Name = "First name")]
        [StringLength(50, ErrorMessage = "{0} must be shorter than {1} characters long.")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "{0} is required.")]
        [Display(Name = "Last name")]
        [StringLength(50, ErrorMessage = "{0} must be shorter than {1} characters long.")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "{0} is required.")]
        [EmailAddress(ErrorMessage = "{0} field does not contain valid email address.")]
        [Display(Name = "Email address")]
        public string Email { get; set; }

        [Required(ErrorMessage = "{0} is required.")]
        [StringLength(20, ErrorMessage = "{0} must be at least {2} and shorter than {1} characters long.", MinimumLength = 3)]
        [Display(Name = "Login")]
        public string Login { get; set; }

        [Required(ErrorMessage = "{0} is required.")]
        [StringLength(20, ErrorMessage = "{0} must be at least {2} and shorter than {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [Compare("Password", ErrorMessage = "Password and confirmation password do not match.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]        
        public string ConfirmPassword { get; set; }
    }

    public class SignInViewModel
    {
        [Required(ErrorMessage = "{0} is required.")]
        [Display(Name = "Login")]
        public string Login { get; set; }

        [Required(ErrorMessage = "{0} is required.")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [Display(Name = "Remember Me")]
        public bool RememberMe { get; set; }
    }

    public class ProfileEditViewModel
    {
        [Required(ErrorMessage = "{0} is required.")]
        [Display(Name = "First name")]
        [StringLength(50, ErrorMessage = "{0} must be shorter than {1} characters long.")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "{0} is required.")]
        [Display(Name = "Last name")]
        [StringLength(50, ErrorMessage = "{0} must be shorter than {1} characters long.")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "{0} is required.")]
        [EmailAddress(ErrorMessage = "{0} field does not contain valid email address.")]
        [Display(Name = "Email address")]
        public string Email { get; set; }

        [Display(Name = "Idle timeout (minutes)")]
        public int IdleTimeout { get; set; }
    }

    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "{0} is required.")]
        [Display(Name = "Old password")]
        public string OldPassword { get; set; }

        [Required(ErrorMessage = "{0} is required.")]
        [StringLength(20, ErrorMessage = "{0} must be at least {2} and shorter than {1} characters long.", MinimumLength = 6)]
        [Display(Name = "New password")]
        public string Password { get; set; }

        [Compare("Password", ErrorMessage = "New password and confirmation password do not match.")]
        [Display(Name = "Confirm new password")]       
        public string ConfirmPassword { get; set; }
    }
}