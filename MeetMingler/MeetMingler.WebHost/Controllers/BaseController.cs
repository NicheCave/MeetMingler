using MeetMingler.BLL.Services.Contracts;
using MeetMingler.DAL.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MeetMingler.WebHost.Controllers;

public class BaseController(IAuthService authService, ICurrentUser currentUser) : Controller
{
    public override async Task OnActionExecutionAsync(ActionExecutingContext filterContext,
        ActionExecutionDelegate next)
    {
        var hasAllowAnonymous = filterContext.ActionDescriptor.EndpointMetadata
            .Any(em => em.GetType() == typeof(AllowAnonymousAttribute));

        if (!hasAllowAnonymous)
        {
            var user = await authService.AuthenticateUserAsync(User, filterContext.HttpContext.RequestAborted);
            if (user == null)
            {
                filterContext.Result = Unauthorized();
                return;
            }

            currentUser.User = user;
        }

        await base.OnActionExecutionAsync(filterContext, next);
    }
}