namespace HrmSystem.Application.Common.Interfaces.Messaging;

/*
    //!       - The value behind having a IBaseCommand Interface; I can apply a generic constraints in my pipeline behaviors, for example I can have a validation behavior that only applies to Commands, and not to Queries (if I have a separate IBaseQuery interface). This way I can have more control over which behaviors apply to which types of requests.
    //!       - This is useful when we implement the Cross-Cutting Concerns (like validation, logging, caching, etc) as pipeline behaviors (decorators) in MediatR. We can apply certain behaviors only to Commands, and not to Queries, by using the IBaseCommand interface as a marker interface.
 */
public interface IBaseCommand : IBaseRequest;
