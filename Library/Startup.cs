using Microsoft.Owin;
using Owin;
using Library.Models.Entities;

[assembly: OwinStartupAttribute(typeof(Library.Startup))]
namespace Library
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
