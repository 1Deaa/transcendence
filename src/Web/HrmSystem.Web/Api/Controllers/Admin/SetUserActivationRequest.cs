using System.ComponentModel.DataAnnotations;

namespace HrmSystem.Web.Api.Controllers.Admin;

public sealed record SetUserActivationRequest([property: Required] bool? IsActive);
