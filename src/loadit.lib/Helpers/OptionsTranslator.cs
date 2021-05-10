using System;
using Humanizer;

namespace Loadit.Helpers
{
    internal static class OptionsTranslator
    {
        public static string TranslateOptions(LoadOptions options)
        {
            if (options.Iterations.HasValue)
            {
                return $"Scenario: {options.Iterations} iterations shared between {options.VUs} VUs" ;    
            }
            return $"Scenario: {options.VUs} looping VUs for {options.Duration.Humanize(5)}" ;
        }
    }
}