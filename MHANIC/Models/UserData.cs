using System.ComponentModel.DataAnnotations;

namespace MHANIC.Models
{
    public class UserData
    {
        [Key]
        [Required(ErrorMessage = "Please enter your mobile number.")]
        public Int32 Mobile_No { get; set; }

        [Required(ErrorMessage = "Please enter your Name.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Please enter your Email.")]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        public string Email_Id { get; set; }

        [Required(ErrorMessage = "Please enter your Address.")]
        public string Address { get; set; }

        public string? PhotoName { get; set; }
    }
}
