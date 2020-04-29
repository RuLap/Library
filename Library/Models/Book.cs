using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using Library.Models;
using Library.Controllers;
using Library.Models.Entities;
using System.ComponentModel.DataAnnotations;

namespace Library.Models
{
    public class Book
    {
        [HiddenInput(DisplayValue = false)]
        [Key]
        public int BookId { get; set; }

        [Display(Name = "Название")]
        [Required(ErrorMessage = "Введите название книги")]
        public string Name { get; set; }

        [Display(Name = "Авторы")]
        [Required(ErrorMessage = "Введите имена авторов")]
        public string Authors { get; set; }

        [HiddenInput(DisplayValue = false)]
        public byte[] Photo { get; set; }

        public string UserId { get; set; }

        public DateTime? GiveDate { get; set; }

        public ApplicationUser User { get; set; }
    }
}