using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Library.Models.Entities;

namespace Library.Models
{
    public interface ILibraryRepository
    {
        IEnumerable<Book> Books { get; }
        IEnumerable<ApplicationUser> Users { get; }

        ApplicationUser GetUserById(string id);

        Book GetBookById(int id);

        void RemoveBookOwner(Book book);

        void AddBookOwner(Book book, ApplicationUser user);

        void SaveBook(Book book);

        void RemoveBook(Book book);

        void SaveUser(ApplicationUser user);

        void RemoveUser(ApplicationUser user);
    }
}