using HrmSystem.Application.Common.Interfaces.Messaging;

namespace HrmSystem.Application.Features.Imports.CreateImportJob;

//? FileContent arrives as bytes — the controller owns IFormFile; Application stays HTTP-free.
public sealed record CreateImportJobCommand(string FileName, byte[] FileContent)
    : ICommand<string>;
