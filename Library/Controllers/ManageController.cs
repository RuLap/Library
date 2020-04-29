using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using System.Data.Entity;
using Microsoft.Owin.Security;
using Library.Models;
using System.IO;
using Vereyon.Web;
using System.Collections.Generic;

namespace Library.Controllers
{
    [Authorize]
    public class ManageController : Controller
    {
        #region Variables
        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;
        private ILibraryRepository repository;
        private int pageSize = 5;
        #endregion

        #region Constructors
        public ManageController(ILibraryRepository repo)
        {
            repository = repo;
        }

        public ManageController(ApplicationUserManager userManager, ApplicationSignInManager signInManager, ILibraryRepository repo)
        {
            UserManager = userManager;
            SignInManager = signInManager;
            repository = repo;
        }
        #endregion

        #region Properties
        public ApplicationSignInManager SignInManager
        {
            get
            {
                return _signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>();
            }
            private set 
            { 
                _signInManager = value; 
            }
        }

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }
        #endregion

        #region Index Methods    

        [Authorize(Roles = "admin")]
        public ActionResult AdminIndex(string searchString, int page = 1)
        {
            Session["LastAction"] = "AdminIndex";
            var model = ConstructDefaultAdminModel();
            if (!String.IsNullOrEmpty(searchString))
            {
                model.UsersViewModel.Users = model.UsersViewModel.Users.Where(x => 
                    (x.FirstName + x.LastName).ToLower().Contains(searchString.ToLower()));
                model.UsersViewModel.PagingInfo.TotalItems = model.UsersViewModel.Users.Count();
            }
            model.UsersViewModel.Users = model.UsersViewModel.Users.Skip((page - 1) * pageSize).Take(pageSize);
            return View(model);
        }

        [HttpGet]
        [Authorize(Roles = "user")]
        public ActionResult UserIndex()
        {
            Session["LastAction"] = "UserIndex";
            return View(ConstructDefaultUserModel());
        }
        #endregion

        #region Admin Books Interactions

        /// <summary>
        /// List of all books in library for admin
        /// </summary>
        /// <returns></returns>
        [Authorize(Roles = "admin")]
        public ActionResult AllBooksList(bool sortByName = false, bool sortByAuthors = false, bool sortByStatus = false)
        {
            Session["LastListAction"] = "AllBooksList";
            Session["LastAction"] = Session["LastListAction"].ToString();
            Session["Controller"] = "Manage";

            if (sortByName)
            {
                return View(repository.Books.OrderBy(b => b.Name));
            }
            if (sortByAuthors)
            {
                return View(repository.Books.OrderBy(b => b.Authors));
            }
            if (sortByStatus)
            {
                var repo = repository.Books.Where(b => b.UserId != null).ToList();
                repo.AddRange(repository.Books.Where(b => b.UserId == null));
                return View(repo);
            }
            return View(repository.Books);
        }

        [Authorize(Roles = "admin")]
        public ActionResult AcceptBook(int id)
        {
            var book = repository.Books.FirstOrDefault(x => x.BookId == id);
            var user = repository.Users.FirstOrDefault(x => x.Id == book.UserId);

            try
            {
                repository.RemoveBookOwner(book);

                FlashMessage.Confirmation("Книга успешно принята");
                return RedirectToAction("AdminConcreteUser", new { userId = user.Id });
            }
            catch
            {
                FlashMessage.Danger($"Не удалось принять книгу. Попробуйте позже или обратитесь в поддержку.");
                return RedirectToAction("AdminConcreteUser", new { userId = user.Id });
            }
        }

        /// <summary>
        /// List of users to choose one for give a book
        /// </summary>
        /// <param name="bookId">Book ID</param>
        /// <param name="searchString">Search filter</param>
        /// <param name="page">Current page number</param>
        /// <returns></returns>
        [Authorize(Roles = "admin")]
        public ActionResult ChooseBookGetter(int bookId, string searchString, int page = 1)
        {
            var model = new UsersListViewModel
            {
                Users = repository.Users.Where(x => x.Id != System.Web.HttpContext.Current.User.Identity.GetUserId()),
                PagingInfo = new PagingInfo
                {
                    CurrentPage = page,
                    ItemsPerPage = pageSize,
                    TotalItems = repository.Users.Count() - 1 
                }
            };

            if (!String.IsNullOrEmpty(searchString))
            {
                model.Users = model.Users.Where(x =>
                    (x.FirstName + x.LastName).ToLower().Contains(searchString.ToLower()));
                model.PagingInfo.TotalItems = model.Users.Count();
            }
            model.Users = model.Users.Skip((page - 1) * pageSize).Take(pageSize);

            ViewBag.BookId = bookId;

            return View(model);
        }

        [Authorize(Roles = "admin")]
        public ActionResult GiveBook(int bookId, string userId)
        {
            var user = repository.Users.FirstOrDefault(x => x.Id == userId);
            var book = repository.GetBookById(bookId);

            if (book.User != null)
            {
                Vereyon.Web.FlashMessage.Danger("Эта книга уже выдана");
                return RedirectToAction("AdminConcreteBook", new { id = book.BookId });
            }

            try
            {
                repository.AddBookOwner(book, user);

                FlashMessage.Confirmation("Книга успешно выдана");
                return RedirectToAction("AdminConcreteBook", new { id = book.BookId });
            }
            catch
            {
                FlashMessage.Danger("Не удалось выдать книгу. Попробуйте позже или обратитесь в поддержку.");
                return RedirectToAction("AdminConcreteBook", new { id = book.BookId });
            }
        }

        [HttpGet]
        [Authorize(Roles = "admin")]
        public ActionResult AdminEditBook(int id)
        {
            return View(repository.GetBookById(id));
        }

        [HttpPost]
        [Authorize(Roles = "admin")]
        public ActionResult AdminEditBook(Book book, HttpPostedFileBase uploadPhoto)
        {
            byte[] photo = null;
            if (ModelState.IsValid && uploadPhoto != null)
            {
                using (var binaryReader = new BinaryReader(uploadPhoto.InputStream))
                {
                    photo = binaryReader.ReadBytes(uploadPhoto.ContentLength);
                }
                book.Photo = photo;
            }
            try
            {
                repository.SaveBook(book);

                FlashMessage.Confirmation("Книга успешно добавлена/отредактированна");
                return RedirectToAction(Session["LastAction"].ToString(), new { id = book.BookId });
            }
            catch
            {
                FlashMessage.Danger("Не удалось отредактировать или добавить книгу. Попробуйте позже или обратитесь в поддержку.");
                return RedirectToAction(Session["LastAction"].ToString(), new { id = book.BookId });
            }
        }

        [HttpPost]
        [Authorize(Roles = "admin")]
        public ActionResult AdminDeleteBook(int id)
        {
            try
            {
                repository.RemoveBook(repository.GetBookById(id));
                Vereyon.Web.FlashMessage.Confirmation("Книга успешно удалена");
                return RedirectToAction("AllBooksList");
            }
            catch(Exception ex)
            {
                FlashMessage.Danger($"{ex.Message}");
                return RedirectToAction("AllBooksList");
            }
        }

        [HttpGet]
        [Authorize(Roles = "admin")]
        public ActionResult AdminConcreteBook(int id)
        {
            Session["LastAction"] = "AdminConcreteBook";

            return View(repository.GetBookById(id));
        }

        [HttpGet]
        [Authorize(Roles = "admin")]
        public ActionResult AdminAddBook()
        {
            return View("AdminEditBook", new Book { BookId = 0 });
        }

        [HttpPost]
        [Authorize(Roles = "admin")]
        public ActionResult AdminAddBook(Book book)
        {
            try
            {
                repository.SaveBook(book);
                FlashMessage.Confirmation("Книга успешно добавлена/отредактированна");
                return RedirectToAction(Session["LastAction"].ToString(), new { id = book.BookId });
            }
            catch
            {
                FlashMessage.Danger("Не удалось отредактировать или добавить книгу. Попробуйте позже или обратитесь в поддержку.");
                return RedirectToAction(Session["LastAction"].ToString(), new { id = book.BookId });
            }
        }
        #endregion

        #region Admin Users Interactions

        [HttpGet]
        [Authorize(Roles = "admin")]
        public ActionResult AllUsersList(string searchString, int page = 1)
        {
            var model = new UsersListViewModel
            {
                Users = repository.Users
                    .Where(x => x.Id != User.Identity.GetUserId())
                    .OrderBy(x => x.FirstName),
                PagingInfo = new PagingInfo
                {
                    CurrentPage = page,
                    ItemsPerPage = pageSize,
                    TotalItems = repository.Users.Count() - 1
                }
            };
            if (!String.IsNullOrEmpty(searchString))
            {
                model.Users = model.Users.Where(x => (x.FirstName + x.LastName + x.Email).ToLower().Contains(searchString));
            }
            model.Users = model.Users.Skip((page - 1) * pageSize).Take(pageSize);
            return View(model);
        }

        public ActionResult AdminConcreteUser(string userId)
        {
            Session["LastAction"] = "AdminConcreteUser";
            return View(repository.GetUserById(userId));
        }
        #endregion

        #region User Editing
        [HttpGet]
        [Authorize]
        public ActionResult EditUser(string userId)
        {
            return View(repository.Users.FirstOrDefault(x => x.Id == userId));
        }

        [HttpPost]
        [Authorize]
        public ActionResult EditUser(ApplicationUser user)
        {
            try
            {
                repository.SaveUser(user);

                Vereyon.Web.FlashMessage.Confirmation("Информация успешно отредактированна");
                if(user.Id != User.Identity.GetUserId())
                    return ViewByActionName("AdminConcreteUser", "UserIndex", user.Id);
                return ViewByActionName("AdminIndex", "UserIndex");
            }
            catch(Exception ex)
            {
                Vereyon.Web.FlashMessage.Confirmation($"{ex.Message}");
                return ViewByActionName("AdminIndex", "UserIndex");
            }
        }

        [HttpPost]
        [Authorize(Roles = "admin")]
        public ActionResult AdminDeleteUser(string userId)
        {
            try
            {
                repository.RemoveUser(repository.GetUserById(userId));
                FlashMessage.Confirmation("Пользователь успешно удален");
                return RedirectToAction("AllUsersList");
            }
            catch (Exception ex)
            {
                FlashMessage.Danger($"{ex.Message}");
                return RedirectToAction("AllUsersList");
            }
        }
        #endregion

        #region AutoGenerated Account Staff
        //
        // POST: /Manage/RemoveLogin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RemoveLogin(string loginProvider, string providerKey)
        {
            ManageMessageId? message;
            var result = await UserManager.RemoveLoginAsync(User.Identity.GetUserId(), new UserLoginInfo(loginProvider, providerKey));
            if (result.Succeeded)
            {
                var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
                if (user != null)
                {
                    await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                }
                message = ManageMessageId.RemoveLoginSuccess;
            }
            else
            {
                message = ManageMessageId.Error;
            }
            return RedirectToAction("ManageLogins", new { Message = message });
        }

        //
        // GET: /Manage/AddPhoneNumber
        public ActionResult AddPhoneNumber()
        {
            return View();
        }

        //
        // POST: /Manage/AddPhoneNumber
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AddPhoneNumber(AddPhoneNumberViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            // Generate the token and send it
            var code = await UserManager.GenerateChangePhoneNumberTokenAsync(User.Identity.GetUserId(), model.Number);
            if (UserManager.SmsService != null)
            {
                var message = new IdentityMessage
                {
                    Destination = model.Number,
                    Body = "Your security code is: " + code
                };
                await UserManager.SmsService.SendAsync(message);
            }
            return RedirectToAction("VerifyPhoneNumber", new { PhoneNumber = model.Number });
        }

        //
        // POST: /Manage/EnableTwoFactorAuthentication
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EnableTwoFactorAuthentication()
        {
            await UserManager.SetTwoFactorEnabledAsync(User.Identity.GetUserId(), true);
            var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            if (user != null)
            {
                await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
            }
            return RedirectToAction("Index", "Manage");
        }

        //
        // POST: /Manage/DisableTwoFactorAuthentication
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DisableTwoFactorAuthentication()
        {
            await UserManager.SetTwoFactorEnabledAsync(User.Identity.GetUserId(), false);
            var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            if (user != null)
            {
                await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
            }
            return RedirectToAction("Index", "Manage");
        }

        //
        // GET: /Manage/VerifyPhoneNumber
        public async Task<ActionResult> VerifyPhoneNumber(string phoneNumber)
        {
            var code = await UserManager.GenerateChangePhoneNumberTokenAsync(User.Identity.GetUserId(), phoneNumber);
            // Send an SMS through the SMS provider to verify the phone number
            return phoneNumber == null ? View("Error") : View(new VerifyPhoneNumberViewModel { PhoneNumber = phoneNumber });
        }

        //
        // POST: /Manage/VerifyPhoneNumber
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> VerifyPhoneNumber(VerifyPhoneNumberViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var result = await UserManager.ChangePhoneNumberAsync(User.Identity.GetUserId(), model.PhoneNumber, model.Code);
            if (result.Succeeded)
            {
                var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
                if (user != null)
                {
                    await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                }
                return RedirectToAction("Index", new { Message = ManageMessageId.AddPhoneSuccess });
            }
            // If we got this far, something failed, redisplay form
            ModelState.AddModelError("", "Failed to verify phone");
            return View(model);
        }

        //
        // POST: /Manage/RemovePhoneNumber
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RemovePhoneNumber()
        {
            var result = await UserManager.SetPhoneNumberAsync(User.Identity.GetUserId(), null);
            if (!result.Succeeded)
            {
                return RedirectToAction("Index", new { Message = ManageMessageId.Error });
            }
            var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            if (user != null)
            {
                await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
            }
            return RedirectToAction("Index", new { Message = ManageMessageId.RemovePhoneSuccess });
        }

        //
        // GET: /Manage/ChangePassword
        public ActionResult ChangePassword()
        {
            return View();
        }

        //
        // POST: /Manage/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var result = await UserManager.ChangePasswordAsync(User.Identity.GetUserId(), model.OldPassword, model.NewPassword);
            if (result.Succeeded)
            {
                var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
                if (user != null)
                {
                    await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                }
                return RedirectToAction("Index", new { Message = ManageMessageId.ChangePasswordSuccess });
            }
            AddErrors(result);
            return View(model);
        }

        //
        // GET: /Manage/SetPassword
        public ActionResult SetPassword()
        {
            return View();
        }

        //
        // POST: /Manage/SetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SetPassword(SetPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await UserManager.AddPasswordAsync(User.Identity.GetUserId(), model.NewPassword);
                if (result.Succeeded)
                {
                    var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
                    if (user != null)
                    {
                        await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                    }
                    return RedirectToAction("Index", new { Message = ManageMessageId.SetPasswordSuccess });
                }
                AddErrors(result);
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Manage/ManageLogins
        public async Task<ActionResult> ManageLogins(ManageMessageId? message)
        {
            ViewBag.StatusMessage =
                message == ManageMessageId.RemoveLoginSuccess ? "The external login was removed."
                : message == ManageMessageId.Error ? "An error has occurred."
                : "";
            var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            if (user == null)
            {
                return View("Error");
            }
            var userLogins = await UserManager.GetLoginsAsync(User.Identity.GetUserId());
            var otherLogins = AuthenticationManager.GetExternalAuthenticationTypes().Where(auth => userLogins.All(ul => auth.AuthenticationType != ul.LoginProvider)).ToList();
            ViewBag.ShowRemoveButton = user.PasswordHash != null || userLogins.Count > 1;
            return View(new ManageLoginsViewModel
            {
                CurrentLogins = userLogins,
                OtherLogins = otherLogins
            });
        }

        //
        // POST: /Manage/LinkLogin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LinkLogin(string provider)
        {
            // Request a redirect to the external login provider to link a login for the current user
            return new AccountController.ChallengeResult(provider, Url.Action("LinkLoginCallback", "Manage"), User.Identity.GetUserId());
        }

        //
        // GET: /Manage/LinkLoginCallback
        public async Task<ActionResult> LinkLoginCallback()
        {
            var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync(XsrfKey, User.Identity.GetUserId());
            if (loginInfo == null)
            {
                return RedirectToAction("ManageLogins", new { Message = ManageMessageId.Error });
            }
            var result = await UserManager.AddLoginAsync(User.Identity.GetUserId(), loginInfo.Login);
            return result.Succeeded ? RedirectToAction("ManageLogins") : RedirectToAction("ManageLogins", new { Message = ManageMessageId.Error });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _userManager != null)
            {
                _userManager.Dispose();
                _userManager = null;
            }

            base.Dispose(disposing);
        }
        #endregion

        #region Helpers
        // Used for XSRF protection when adding external logins

        private ActionResult ViewByActionName(string adminActionName, string userActionName, object adminModel = null)
        {
            if (User.IsInRole("admin"))
            {
                if(adminModel is null)
                {
                    return RedirectToAction(adminActionName);
                }
                return RedirectToAction(adminActionName, new { userId = adminModel });
            }
            else
            {
                return RedirectToAction(userActionName);
            }
        }

        private UserIndexViewModel ConstructDefaultUserModel()
        {
            var user = repository.Users.FirstOrDefault(x => x.Id == User.Identity.GetUserId());

            var model = new UserIndexViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Books = user.Books
            };
            return model;
        }

        private AdminIndexViewModel ConstructDefaultAdminModel()
        {
            var user = repository.Users.FirstOrDefault(x => x.Id == User.Identity.GetUserId());
            var model = new AdminIndexViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                UsersViewModel = new UsersListViewModel
                {
                    Users = repository.Users.Where(x => x.Id != user.Id),
                    PagingInfo = new PagingInfo
                    {
                        CurrentPage = 1,
                        ItemsPerPage = pageSize,
                        TotalItems = repository.Books.Count()
                    }
                }
            };
            return model;
        }

        private const string XsrfKey = "XsrfId";

        private IAuthenticationManager AuthenticationManager
        {
            get
            {
                return HttpContext.GetOwinContext().Authentication;
            }
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }

        private bool HasPassword()
        {
            var user = UserManager.FindById(User.Identity.GetUserId());
            if (user != null)
            {
                return user.PasswordHash != null;
            }
            return false;
        }

        private bool HasPhoneNumber()
        {
            var user = UserManager.FindById(User.Identity.GetUserId());
            if (user != null)
            {
                return user.PhoneNumber != null;
            }
            return false;
        }

        public enum ManageMessageId
        {
            AddPhoneSuccess,
            ChangePasswordSuccess,
            SetTwoFactorSuccess,
            SetPasswordSuccess,
            RemoveLoginSuccess,
            RemovePhoneSuccess,
            Error
        }

#endregion
    }
}