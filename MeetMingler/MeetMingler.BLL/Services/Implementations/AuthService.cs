using System.Security.Claims;
using MeetMingler.BLL.Caching;
using MeetMingler.BLL.Models.User;
using MeetMingler.BLL.Services.Contracts;
using MeetMingler.DAL.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace MeetMingler.BLL.Services.Implementations;

public class AuthService(
    SignInManager<User> signInManager,
    UserManager<User> userManager,
    IMemoryCache cache) : IAuthService
{
    private static readonly TimeSpan UserCacheTtl = TimeSpan.FromMinutes(2);

    public async Task SignInAsync(UserLoginIM loginIm, CancellationToken cf = default)
    {
        var user = await userManager.Users.FirstOrDefaultAsync(e => e.Email == loginIm.Email, cf);

        if (user == null)
            throw new IdentityException([
                new IdentityError
                {
                    Code = "Invalid Credentials",
                    Description = "Invalid email or password."
                }
            ]);

        var result =
            await signInManager.PasswordSignInAsync(user, loginIm.Password, true, false);
        
        if (!result.Succeeded)
            throw new IdentityException([
                new IdentityError() { Code = "InvalidCredentials", Description = "Invalid email or password." }
            ]);
    }

    public async Task SignOutAsync(CancellationToken cf = default)
    {
        await signInManager.SignOutAsync();
    }

    public async Task<User?> AuthenticateUserAsync(ClaimsPrincipal userClaims, CancellationToken cf = default)
    {
        if (!Guid.TryParse(userManager.GetUserId(userClaims), out var userId))
            return null;

        var key = CacheKeys.User(userId);
        if (cache.TryGetValue(key, out User? user))
            return user;

        user = await userManager.FindByIdAsync(userId.ToString());
        if (user != null)
            cache.Set(key, user, UserCacheTtl);

        return user;
    }
}