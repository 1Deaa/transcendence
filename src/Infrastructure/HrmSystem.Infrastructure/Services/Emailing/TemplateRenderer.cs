using System.Text.RegularExpressions;
using HrmSystem.Application.Common.Interfaces.Emailing;

namespace HrmSystem.Infrastructure.Services.Emailing;

/*
    //? Implements ITemplateRenderer using a compiled regex for single-pass {{Key}} → value substitution.
    //?
    //? Pattern: {{Key}} where Key is one or more characters that are not braces.
    //?
    //> Example:
    //>   template  = "Hi {{UserName}}, your habit '{{HabitName}}' was created."
    //>   variables = { "UserName": "Ahmad", "HabitName": "Drink 2L Water" }
    //>   result    = "Hi Ahmad, your habit 'Drink 2L Water' was created."
    //?
    //> Keys are matched case-sensitively.
    //> Tokens with no matching dictionary entry are left unchanged in the output.
    //>   e.g. {{UnknownKey}} remains {{UnknownKey}} — no silent data loss.
    //
    //! RegexOptions.Compiled trades a one-time JIT cost for faster repeated execution.
    //!  The matchTimeout (100 ms) guards against catastrophic backtracking on malformed templates.
    //!  Register as Singleton — this class is stateless and Regex is thread-safe.
*/
internal sealed class TemplateRenderer : ITemplateRenderer
{
    /*
        //!    (🚨)  You are using a verbatim string literal (@), which means backslashes are treated as literal characters rather than escape characters (saving you from "leaning toothpick syndrome" like \\\\{\\\\{).
        //!    (🚨)  \{\{ and \}\}:
        //?        (🚨)  The braces are escaped with backslashes because { and } normally define quantifiers in regex (like {1,3}). Escaping them tells the engine to look for literal double braces.
        //!    (🚨)  ([^{}]+): This is the heart of the engine.
        //?        (🚨)  The parentheses () create a Capture Group. If you match {{Username}}, the whole regex matches the entire string, but the capture group explicitly extracts just Username.
        //?        (🚨)  [^{}]+ is a negated character class. It matches one or more (+) of any character that is not (^) an opening or closing brace `{}`. This is much safer and more efficient than using `.+` (which can cause "greedy" matching bugs where it swallows multiple placeholders at once).
        //!    (🚨) The Performance Engine: `RegexOptions.Compiled`
        //?        (🚨) By default, .NET interprets regular expressions on the fly. By adding RegexOptions.Compiled, you are telling the .NET compiler to take this regex pattern and compile it down into raw, underlying MSIL (Microsoft Intermediate Language) assembly code.
        //?        (🚨) The Trade-off: It takes slightly longer to start up (a few milliseconds to compile the first time the class is loaded), but it executes significantly faster. Because this field is static, you pay that startup cost exactly once, reaping the performance benefits on every subsequent match.
        //!    (🚨) The Security Shield: matchTimeout
        //?        (🚨) This is arguably the most important feature for enterprise applications. It protects your application from ReDoS (Regular Expression Denial of Service) attacks.
        //?        (🚨) Sometimes, malformed input strings can cause a regex engine to enter a state of "catastrophic backtracking," where evaluating the string takes exponential time, locking up your CPU and crashing your server.
        //?        (🚨) TimeSpan.FromMilliseconds(100) tells the regex engine: "If you can't figure out if this string matches within 1/10th of a second, give up and throw a RegexMatchTimeoutException." It acts as a strict circuit breaker.
        //?    \{\{          — opening double braces: {{
        //?    ([^{}]+)      — capture group: one or more chars that are NOT { or }
        //?    \}\}          — closing double braces: }}
    */
    private static readonly Regex PlaceholderPattern = new Regex(
        @"\{\{([^{}]+)\}\}",
        RegexOptions.Compiled,
        matchTimeout: TimeSpan.FromMilliseconds(100)
    );

    /*
         //! (🚨)     The regex engine scans the template string.
         //! (🚨)     Every time it hits a {{...}} pattern, it pauses and hands that specific Match object over to your lambda.
         //! (🚨)     The regex engine scans the template string.
         //? (🚨)     Extracting the Target: match.Groups[1].ValueS
         //*    (🚨)     This is where the capture group ([^{}]+) from your regex pays off.
         //*    (🚨)     If the engine finds {{Username}}, the match.Value (the full match) is {{Username}}.
         //*    (🚨)     However, `match.Groups[1].Value` looks inside the parentheses of your regex, ignoring the braces, and grabs just Username.
         //*    (🚨)     You now have the exact key needed to query your dictionary, without needing to write clunky code to Trim() or strip out the curly braces manually.
         //?
    */
    public string Render(string template, IReadOnlyDictionary<string, string> variables)
    {
        return PlaceholderPattern.Replace(
            template,
            match =>
            {
                string key = match.Groups[1].Value;
                return variables.TryGetValue(key, out string? value) ? value : match.Value;
            }
        );
    }
}
