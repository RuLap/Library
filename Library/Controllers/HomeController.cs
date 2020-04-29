using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Library.Models;
using Library.Models.Entities;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace Library.Controllers
{
    public class HomeController : Controller
    {
        private ILibraryRepository repository;
        public int pageSize = 5;

        public HomeController(ILibraryRepository repo)
        {
            repository = repo;
        }
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        [Authorize]
        public async Task<ActionResult> ListBooks(string searchString = "", int page = 1)
        {
            Session["LastListAction"] = "ListBooks";
            Session["Controller"] = "Home";
            Session["Search"] = searchString;
            Session["Page"] = page.ToString();

            BooksListViewModel model = new BooksListViewModel
            {
                Books = repository.Books
                    .OrderBy(b => b.BookId),
                PagingInfo = new PagingInfo
                {
                    CurrentPage = page,
                    ItemsPerPage = pageSize,
                    TotalItems = repository.Books.Count()
                }
            };
            if (!String.IsNullOrEmpty(searchString))
            {
                model.Books = model.Books.Where(x => 
                    (x.Name.ToLower() + x.Authors.ToLower()).Contains(searchString.ToLower()));
                model.PagingInfo.TotalItems = model.Books.Count();
            }
            model.Books = model.Books.Skip((page - 1) * pageSize).Take(pageSize); 
            return View(model);
        }

        [HttpGet]
        public FileContentResult GetPhoto(int id)
        {
            Book book = repository.Books.FirstOrDefault(x => x.BookId == id);
            if(book.Photo != null)
            {
                return File(book.Photo, "image/png");
            }
            else
            {
                return File(System.IO.File.ReadAllBytes(System.Web.HttpContext.Current.Server.MapPath("~/Content/Images/NoPhoto.png")), "image/png");
            }
        }
    }
}