﻿using LocalEducation.Services.EducationRepositories.Interfaces;
using LocalEducation.Services.Extensions;
using LocalEducation.Core.Contracts;
using LocalEducation.Core.Utilities;
using LocalEducation.Core.Constants;
using Microsoft.EntityFrameworkCore;
using LocalEducation.Data.Contexts;
using LocalEducation.Core.Entities;
using LocalEducation.Core.Queries;

namespace LocalEducation.Services.EducationRepositories;

public class UserRepository : IUserRepository
{
    private readonly LocalEducationDbContext _dbContext;
    private readonly IPasswordHasher _hasher;

    public UserRepository(LocalEducationDbContext context, IPasswordHasher hasher)
    {
        _dbContext = context;
        _hasher = hasher;
    }

    public async Task<LoginResult> LoginAsync(User userLogin, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Set<User>()
            .Include(s => s.Roles)
            .FirstOrDefaultAsync(u => 
                u.Username.Equals(userLogin.Username), cancellationToken);

        var result = new LoginResult
        {
            User = user
        };

        if (user == null)
        {
            result.Status = LoginStatus.Username;
        }
        else if (_hasher.VerifyPassword(user.Password, userLogin.Password))
        {
            result.Status = LoginStatus.Success;
        }
        else
        {
            result.Status = LoginStatus.Password;
        }

        return result;
    }

    public async Task<bool> DeleteRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<UserLogin>()
            .Where(x => x.RefreshToken.Equals(refreshToken))
            .ExecuteDeleteAsync(cancellationToken) > 0;
    }

    public async Task<UserLogin> GetRefreshTokenAsync(
        string refreshToken,
        string userAgent,
        string ipAddress,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<UserLogin>()
            .FirstOrDefaultAsync(s =>
                s.RefreshToken.Equals(refreshToken) &&
                s.UserAgent.Equals(userAgent), cancellationToken);
    }

    public async Task<User> GetUserByIdAsync(Guid userId, bool getFull = false, CancellationToken cancellationToken = default)
    {
        if (getFull)
        {
            return await _dbContext.Set<User>()
                .Include(s => s.UserLogins)
                .Include(s => s.Roles)
                .FirstOrDefaultAsync(s => s.Id == userId, cancellationToken);
        }
        return await _dbContext.Set<User>().FirstOrDefaultAsync(s => s.Id == userId, cancellationToken);
    }

    public async Task<bool> ChangePasswordAsync(User user, string oldPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        if (user != null && _hasher.VerifyPassword(user.Password, oldPassword))
        {
            user.Password = _hasher.Hash(newPassword);
            _dbContext.Entry(user).State = EntityState.Modified;
            await _dbContext.SaveChangesAsync(cancellationToken);

            return true;
        }

        return false;
    }

    public async Task<User> Register(User user, CancellationToken cancellationToken = default)
    {
        var userExist = await _dbContext.Set<User>().AnyAsync(s => s.Username == user.Username, cancellationToken);
        if (userExist)
        {
            return null;
        }
        user.CreatedDate = DateTime.Now;
        user.Roles = new List<Role>();
        user.Password = _hasher.Hash(user.Password);

        user.Roles = await _dbContext.Set<Role>()
            .Where(s => s.Name == "User").ToListAsync(cancellationToken);

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return user;
    }

    public async Task<Role> GetRoleByNameAsync(string role, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<Role>()
            .Include(s => s.Users)
            .FirstOrDefaultAsync(s => s.Name.Equals(role), cancellationToken);
    }

    public async Task<bool> IsUserExistedAsync(string userName, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<User>()
            .AnyAsync(s => s.Username == userName, cancellationToken);
    }

    public async Task<IList<Role>> GetRolesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<Role>()
            .ToListAsync(cancellationToken);
    }

    public async Task<User> SetUserRolesAsync(Guid userId, IList<Guid> roles, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Set<User>()
            .Include(s => s.Roles)
            .FirstOrDefaultAsync(s => s.Id == userId, cancellationToken);

        UpdateUserRoles(ref user, roles);

        _dbContext.Entry(user).State = EntityState.Modified;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return user;
    }

    public async Task<UserLogin> SetRefreshTokenAsync(Guid userId, UserLogin userLogin, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Set<User>()
            .Include(s => s.Roles)
            .FirstOrDefaultAsync(user =>
                user.Id.Equals(userId), cancellationToken);

        if (user != null)
        {
            var login = await _dbContext.Set<UserLogin>()
                .FirstOrDefaultAsync(s =>
                    s.UserId == userId &&
                    s.UserAgent == userLogin.UserAgent &&
                    s.IpAddress == userLogin.IpAddress, cancellationToken);

            if (login != null)
            {
                login.RefreshToken = userLogin.RefreshToken;
                login.TokenCreated = userLogin.TokenCreated;
                login.TokenExpires = userLogin.TokenExpires;

                _dbContext.Entry(login).State = EntityState.Modified;
            }
            else
            {
                userLogin.CreatedDate = DateTime.Now;
                userLogin.UserId = user.Id;
                _dbContext.UserLogin.Add(userLogin);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            return userLogin;
        }
        return null;
    }

    public async Task<User> GetUserRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var userLogin = await _dbContext.Set<UserLogin>()
            .FirstOrDefaultAsync(s => s.RefreshToken.Equals(refreshToken), cancellationToken);

        if (userLogin == null)
        {
            return null;
        }

        return await _dbContext.Set<User>()
            .Include(s => s.UserLogins)
            .Include(s => s.Roles)
            .FirstOrDefaultAsync(s => s.Id == userLogin.Id, cancellationToken);
    }

    public async Task<User> UpdateProfileAsync(User user, CancellationToken cancellationToken = default)
    {
        if (_dbContext.Set<User>().Any(s => s.Id == user.Id))
        {
            _dbContext.Entry(user).State = EntityState.Modified;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return user;
        }

        return null;
    }

    public async Task<IPagedList<T>> GetPagedUsersAsync<T>(UserQuery condition, IPagingParams pagingParams, Func<IQueryable<User>, IQueryable<T>> mapper,
        CancellationToken cancellationToken = default)
    {
        var users = FilterUser(condition);

        var projectedUsers = mapper(users);

        return await projectedUsers.ToPagedListAsync(pagingParams, cancellationToken);
    }

    public bool UpdateUserRoles(ref User user, IEnumerable<Guid> selectRoles)
    {
        if (selectRoles == null) return false;

        var roles = _dbContext.Roles.ToList();
        var currentRoleNames = new HashSet<Guid>(user.Roles.Select(x => x.Id));

        foreach (var role in roles)
        {
            var enumerable = selectRoles as Guid[] ?? selectRoles.ToArray();
            if (enumerable.ToList().Contains(role.Id))
            {
                if (!currentRoleNames.ToList().Contains(role.Id))
                {
                    user.Roles.Add(role);
                }
            }
            else if (currentRoleNames.ToList().Contains(role.Id))
            {
                user.Roles.Remove(role);
            }
        }
        return true;
    }

    #region Filter

    private IQueryable<User> FilterUser(IUserQuery condition)
    {
        var users = _dbContext.Set<User>()
            .WhereIf(!string.IsNullOrWhiteSpace(condition.Keyword), s =>
                s.Address.Contains(condition.Keyword) ||
                s.Email.Contains(condition.Keyword) ||
                s.Name.Contains(condition.Keyword) ||
                s.Phone.Contains(condition.Keyword) ||
                s.Username.Contains(condition.Keyword));

        return users;

    }
    #endregion
}