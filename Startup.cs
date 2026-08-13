using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(LoopFlow.Startup))]
namespace LoopFlow
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
