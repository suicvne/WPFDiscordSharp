using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace CustomDiscordClient
{
    public static class ResourceHelper
    {
        static public string FindNameFromResource(ResourceDictionary dictionary, object resourceItem)
        {
            foreach (object key in dictionary.Keys)
            {
                if (dictionary[key] == resourceItem)
                {
                    return key.ToString();
                }
            }

            return null;
        }
    }
}
