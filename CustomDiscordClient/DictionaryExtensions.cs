using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomDiscordClient
{
    public static class DictionaryExtensions
    {
        public static T Find<T>(this Dictionary<string, T> inputDictionary, Predicate<T> matchFunction)
        {
            foreach(var kvp in inputDictionary)
            {
                if (matchFunction.Invoke(kvp.Value))
                    return kvp.Value;
            }

            return default;
        }

    }
}
