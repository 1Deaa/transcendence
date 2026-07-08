using Microsoft.AspNetCore.SignalR;

namespace HrmSystem.Web.Hubs;

/*
    //?     Push channel for the PUBLIC status page — server→client only, no client methods.
    //?     HealthProbeJob broadcasts a "StatusChanged" event on confirmed health transitions.
    //!     Anonymous BY DESIGN (like the status endpoints): the payload is the redacted
    //!     PublicStatusResponse — component names + coarse status, nothing internal.
*/
public sealed class StatusHub : Hub;
