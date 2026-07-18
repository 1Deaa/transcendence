using HrmSystem.Application.Common.Interfaces.Messaging;

namespace HrmSystem.Application.Features.Admin.SetUserActivation;

//? Suspends (IsActive = false) or reinstates a workspace account — login is blocked while suspended.
public sealed record SetUserActivationCommand(string UserId, bool IsActive) : ICommand;
