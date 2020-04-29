using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Library.Models;
using System.Text;

namespace Library.HtmlHelpers
{
    public static class UsersListHelper
    {
        public static MvcHtmlString UserLinks(this HtmlHelper html, IEnumerable<ApplicationUser> users, 
            Func<string, string> pageUrl, bool isBooksShown = false)
        {
            StringBuilder result = new StringBuilder();

            foreach (var user in users)
            {
                TagBuilder h4 = new TagBuilder("h4");
                h4.MergeAttribute("style", "padding-top:10px");

                TagBuilder atag = new TagBuilder("a");
                atag.MergeAttribute("href", pageUrl(user.Id));
                atag.InnerHtml = user.FirstName + " " + user.LastName;

                TagBuilder span = new TagBuilder("span");
                span.AddCssClass("badge badge - pill badge - primary");
                span.MergeAttribute("style", "margin-left:5px");
                span.InnerHtml = user.Email;

                h4.InnerHtml = atag.ToString() + span.ToString();

                result.Append(h4.ToString());
                TagBuilder ul = new TagBuilder("ul");
                TagBuilder li;
                if (isBooksShown)
                {
                    if (user.Books != null && user.Books.Count() != 0)
                    {
                        string list = "";
                        foreach (var book in user.Books)
                        {
                            li = new TagBuilder("li");
                            li.InnerHtml = book.Name;
                            list += li.ToString();
                        }
                        ul.InnerHtml = list;
                        result.Append(ul.ToString());
                    }
                }
            }
            return MvcHtmlString.Create(result.ToString()); 
        }
    }
}