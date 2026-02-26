using System;
using System.Collections.Generic;
using System.Linq;

namespace EasyLibrary.Core;

public static class JobParser
{
    public static List<int> ParseSelection(string input, int maxCount)
    {
        var indices = new HashSet<int>();
        var parts = input.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            string cleanPart = part.Trim();
            if (cleanPart.Contains('-'))
            {
                var range = cleanPart.Split('-');
                if (range.Length == 2 && int.TryParse(range[0], out int s) && int.TryParse(range[1], out int e))
                    for (int i = Math.Min(s, e); i <= Math.Max(s, e); i++) indices.Add(i);
            }
            else if (int.TryParse(cleanPart, out int id)) indices.Add(id);
        }
        return indices.Where(i => i >= 0 && i < maxCount).ToList();
    }
}
//Reçoit la chaîne de caractères tapée par l'utilisateur (ex: "1-3;5") et la transforme en une liste d'index numériques C'est une extraction de logique 