using System.ComponentModel.DataAnnotations;

namespace HrmSystem.Web.Api.Controllers.Admin;

/*
    //!     [Required] must target the CONSTRUCTOR PARAMETER, not the property.
    //!     With [property: Required] MVC throws InvalidOperationException while validating
    //!     the record ("validation metadata ... will be ignored"), so every suspend /
    //!     reinstate call died with a 500 before ever reaching the handler.
*/
public sealed record SetUserActivationRequest([Required] bool? IsActive);
