using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;
using System.Configuration;
using Library.Models.Entities;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;

namespace Library.Models
{
    public class LibraryRepository : ILibraryRepository
    {
        public ApplicationDbContext context = new ApplicationDbContext();
        public IEnumerable<Book> Books
        {
            get { return context.Books; }
        }
        public IEnumerable<ApplicationUser> Users 
        {
            get { return context.Users; }
        }

        public LibraryRepository()
        {
            SetUsersBooks();
            SetBooksOwners();
        }

        private void SetUsersBooks()
        {
            foreach (var user in Users)
            {
                var books = Books.Where(x => x.UserId == user.Id).ToList<Book>();
                if (books.Count == 0)
                {
                    user.Books = null;
                }
                else
                {
                    user.Books = books;
                }
            }
        }

        private void SetBooksOwners()
        {
            foreach (var book in Books)
            {
                var user = Users.Where(x => x.Id == book.UserId).ToList<ApplicationUser>();
                if (user.Count == 0)
                {
                    book.User = null;
                }
                else
                {
                    book.User = user[0];
                }
            }
        }

        public Book GetBookById(int id)
        {
            return Books.Single(x => x.BookId == id);
        }

        public ApplicationUser GetUserById(string id)
        {
            return Users.Single(x => x.Id == id);
        }

        public void RemoveBookOwner(Book book)
        {
            book.UserId = null;
            book.GiveDate = null;

            context.Entry(book).State = EntityState.Modified;
            context.SaveChanges();
        }

        public void AddBookOwner(Book book, ApplicationUser user)
        {
            book.UserId = user.Id;
            book.GiveDate = DateTime.Now;

            context.Entry(book).State = EntityState.Modified;
            context.SaveChanges();
        }

        public void SaveBook(Book book)
        {
            if (book.BookId == 0)
            {
                context.Books.Add(book);
                context.Entry(book).State = EntityState.Added;
            }
            else
            {
                var entry = context.Books.Find(book.BookId);

                entry.Name = book.Name;
                entry.Authors = book.Authors;
                entry.Photo = book.Photo;
                context.Entry(entry).State = EntityState.Modified;
            }
            context.SaveChanges();
        }

        public void SaveUser(ApplicationUser user)
        {
            var entry = context.Users.Find(user.Id);

            entry.Email = user.Email;
            entry.UserName = user.Email;
            entry.FirstName = user.FirstName;
            entry.LastName = user.LastName;

            context.Entry(entry).State = EntityState.Modified;
            context.SaveChanges();
        }

        public void RemoveBook(Book book)
        {
            if(book.User != null)
            {
                throw new Exception($"Невозможно удалить, книга на руках у пользователя {book.User.FirstName} {book.User.LastName}.");
            }
            context.Books.Remove(book);
            context.SaveChanges();
        }

        public void RemoveUser(ApplicationUser user)
        {
            if(user.Books != null)
            {
                if (user.Books.Count != 0)
                {
                    throw new Exception("Невозможно удалить, у пользователя есть не возвращенные книги.");
                }
            }
            user.Roles.Clear();
            context.Users.Remove(user);
            context.SaveChanges();
        }
    }
}