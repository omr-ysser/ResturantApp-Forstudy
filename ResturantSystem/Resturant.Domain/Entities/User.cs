using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Resturant.Domain.Common;
using Resturant.Domain.Exceptions;

namespace Resturant.Domain.Entities;

public sealed class User : BaseEntity
{
    private readonly List<UserRole> _userRoles = [];
    private readonly List<RefreshToken> _refreshTokens = [];

    private User()
    {
    }

    public User(
        string fullName,
        string email,
        string passwordHash)
    {
        UpdateProfile(fullName, email);
        ChangePasswordHash(passwordHash);
    }

    public string FullName { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public string NormalizedEmail { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public bool IsActive { get; private set; } = true;

    public IReadOnlyCollection<UserRole> UserRoles =>
        _userRoles.AsReadOnly();

    public IReadOnlyCollection<RefreshToken> RefreshTokens =>
        _refreshTokens.AsReadOnly();

    public void UpdateProfile(string fullName, string email)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new DomainException("Full name is required.");

        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Email is required.");

        FullName = fullName.Trim();
        Email = email.Trim();
        NormalizedEmail = Email.ToUpperInvariant();

        MarkUpdated();
    }

    public void ChangePasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("Password hash is required.");

        PasswordHash = passwordHash;
        MarkUpdated();
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        MarkUpdated();
    }

    public void AssignRole(Guid roleId)
    {
        if (roleId == Guid.Empty)
            throw new DomainException("Role is required.");

        if (_userRoles.Any(item => item.RoleId == roleId))
            return;

        _userRoles.Add(new UserRole(Id, roleId));
        MarkUpdated();
    }

    public void RemoveRole(Guid roleId)
    {
        var userRole = _userRoles
            .SingleOrDefault(item => item.RoleId == roleId);

        if (userRole is null)
            return;

        _userRoles.Remove(userRole);
        MarkUpdated();
    }
}
