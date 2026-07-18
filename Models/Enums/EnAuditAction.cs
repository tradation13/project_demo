namespace IPTS.Models.Enums
{
    public enum EnAuditAction
    {
        LoginSuccess = 1,
        LoginFailed = 2,
        AccountLocked = 3,
        Logout = 4,
        UserCreated = 5,
        UserUpdated = 6,
        UserDeleted = 7,
        PasswordChanged = 8,
        PasswordResetRequested = 9,
        UnauthorizedAccess = 10,
        RoleChanged = 11,
        EntityCreated = 12,
        EntityUpdated = 13,
        EntityDeleted = 14
    }
}
