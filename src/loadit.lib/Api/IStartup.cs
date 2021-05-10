using Microsoft.Extensions.DependencyInjection;

namespace Loadit
{
    public interface IStartup
    {
        public void ConfigureServices(IServiceCollection services);
    }
}