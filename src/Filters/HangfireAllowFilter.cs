using Hangfire.Dashboard;

namespace LazyDan2.Filters;

public class HangfireAllowFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        return true;
    }
}
