namespace HrmSystem.Application.Common.Interfaces.Messaging;

/*
//? Common ancestor for all domain requests (commands and queries).
//? Lets pipeline behaviors constrain to "our requests" without coupling to MediatR's IRequest<T>.
*/
public interface IBaseRequest;
