using AutoMapper;
using MeetMingler.BLL.Caching;
using MeetMingler.BLL.Models.User;
using MeetMingler.BLL.Services.Contracts;
using MeetMingler.DAL.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace MeetMingler.BLL.Services.Implementations;

public class UserService(UserManager<User> userManager, IMapper mapper, IMemoryCache cache)
    : IUserService
{
    public async Task<UserVM?> CreateAsync(UserIM im, CancellationToken cf = default)
    {
        var user = mapper.Map<User>(im);
        var identityResult = await userManager.CreateAsync(user, im.Password);

        if (!identityResult.Succeeded)
            throw new IdentityException(identityResult.Errors);

        return mapper.Map<UserVM>(user);
    }

    public async Task<UserVM?> GetByIdAsync(Guid id, CancellationToken cf = default)
    {
        var user = await userManager.Users.FirstOrDefaultAsync(e => e.Id == id, cancellationToken: cf);
        
        return user == null ? null : mapper.Map<UserVM>(user);
    }
    
    public async Task<bool> UpdateUserPassword(Guid id, PasswordUM passwordUm)
    {
        var user = await userManager.Users.FirstOrDefaultAsync(e => e.Id == id);

        if (user == null)
            return false;

        var result = await userManager.ChangePasswordAsync(user, passwordUm.Current, passwordUm.New);

        if (!result.Succeeded)
            throw new IdentityException(result.Errors);

        cache.Remove(CacheKeys.User(id));
        return true;
    }
}
