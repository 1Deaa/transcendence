namespace HrmSystem.Application.Common.Exceptions;

/*
    //?     Thrown by the TenantWriteGuardInterceptor when a save would cross tenant boundaries:
    //?      - a tenant-owned entity is written while NO tenant context is resolved, or
    //?      - an entity already stamped with tenant A is written under tenant B's context.
    //
    //!     This is an exception (not a Result error) on purpose — a cross-tenant write is
    //!     never a business outcome; it is a programming/security fault that must fail loudly.
*/
public sealed class CrossTenantWriteException : Exception
{
    public CrossTenantWriteException(string message)
        : base(message) { }

    public CrossTenantWriteException()
        : base("A tenant-owned entity was written outside its tenant's context.") { }

    public CrossTenantWriteException(string message, Exception innerException)
        : base(message, innerException) { }
}
