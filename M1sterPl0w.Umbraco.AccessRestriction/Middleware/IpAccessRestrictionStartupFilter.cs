using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace M1sterPl0w.Umbraco.AccessRestriction.Middleware
{
    public class AccessRestrictionStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                app.UseMiddleware<AccessRestrictionMiddleware>();
                next(app);
            };
        }
    }
}
