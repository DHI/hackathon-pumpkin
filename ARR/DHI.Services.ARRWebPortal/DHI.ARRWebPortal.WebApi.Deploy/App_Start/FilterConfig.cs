using System.Web;
using System.Web.Mvc;

namespace DHI.ARRWebPortal.WebApi.Deploy
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }
    }
}
