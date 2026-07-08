namespace HrmSystem.Application.Common.Interfaces.Emailing;

public interface ITemplateRenderer
{
    //? Substitutes every {{Key}} token in template with the matching value from variables.
    //? Keys are matched case-sensitively. Tokens with no matching entry are left unchanged.
    string Render(string template, IReadOnlyDictionary<string, string> variables);
}
