using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SubSyncLib.Logic.Extensions;

namespace SubSyncLib.Logic
{
    public static class FilenameDiff
    {
        public static int IndexOfBestMatch(string needle, string[] haystack)
        {
            var index = 0;
            var scored = new Dictionary<int, double>();
            var input = Path.GetFileNameWithoutExtension(needle);

            foreach (var item in haystack)
            {
                if (item.Equals(input, StringComparison.OrdinalIgnoreCase))
                {
                    return index; // direct match
                }
                scored[index++] = GetDiffScore(input, item);
            }

            // score: the lower, the better.
            if (scored.Count > 0)
            {
                return scored.OrderBy(x => x.Value).First().Key;
            }

            return -1;
        }

        public static T FindBestMatch<T>(string needle, T[] haystack, Func<T, string> stringComparison)
        {
            var index = IndexOfBestMatch(needle, haystack.Select(stringComparison).ToArray());
            return index != -1 ? haystack[index] : default(T);
        }

        public static double GetDiffScore(string a, string b)
        {
            // first iteration is to take all distinct values from a and b
            // then compare the actual content and score it.
            // second iteration takes words into consideration
            var c1 = a.ToLower().ToCharArray();
            var c2 = b.ToLower().ToCharArray();

            var diff = new HashSet<char>(c1);
            diff.SymmetricExceptWith(c2);

            var changes = diff.ToArray(); // c1.Intersect(c2).ToArray();
            var score = 0.0;
            // different chars have different scoring
            // letters are 1.0
            // numbers are 0.75
            // brackets and paranthesis are 0.5
            // spaces are 0.1
            foreach (var change in changes)
            {
                if (change == '[' || change == ']' || change == '(' || change == ')') score += 0.5;
                else if (char.IsDigit(change)) score += 0.75;
                else if (change == ' ') score += 0.1;
                else score += 1.0;
            }

            var l0 = new HashSet<string>();
            a.ToLower().Split(new[] { '.', ' ' }, StringSplitOptions.RemoveEmptyEntries).ForEach(x => l0.Add(x));
            if (l0.Count > 1) l0.Remove(a.ToLower());

            var l1 = new HashSet<string>();
            b.ToLower().Split(new[] { '.', ' ' }, StringSplitOptions.RemoveEmptyEntries).ForEach(x => l1.Add(x));
            if (l1.Count > 1) l1.Remove(b.ToLower());

            var l2 = new HashSet<string>();
            l2.UnionWith(l0);
            l2.UnionWith(l1);

            for (var i = 0; i < l2.Count; i++)
            {
                if (!l0.Contains(l2.ElementAt(i))) score++;
                if (!l1.Contains(l2.ElementAt(i))) score++;
            }

            return score;
        }
    }
}
