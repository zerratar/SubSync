using System;
using System.Collections.Generic;

namespace SubSyncLib.Logic
{
    public class VideoIgnoreFilter : IVideoIgnoreFilter
    {
        private readonly IReadOnlyList<IVideoIgnoreFilter> filters;

        public VideoIgnoreFilter(IEnumerable<string> filters)
        {
            this.filters = BuildFilters(filters);
        }

        private IReadOnlyList<IVideoIgnoreFilter> BuildFilters(IEnumerable<string> items)
        {
            var result = new List<IVideoIgnoreFilter>();

            foreach (var filter in items)
            {
                result.Add(BuildFilter(filter));
            }

            return result;
        }

        private IVideoIgnoreFilter BuildFilter(string filter)
        {
            filter = filter.Replace('\\', '/');
            if (string.IsNullOrEmpty(filter))
            {
                return new PassthroughFilter();
            }

            if (filter.Contains("*"))
            {
                return new FuzzyFilter(filter);
            }

            return new EndsWithFilter(filter);
        }

        public bool Match(string filepath)
        {
            // return true if filter match
            filepath = filepath.Replace('\\', '/');
            foreach (var filter in this.filters)
            {
                if (filter.Match(filepath))
                {
                    return true;
                }
            }

            return false;
        }
    }

    internal class EndsWithFilter : IVideoIgnoreFilter
    {
        private readonly string _filter;

        public EndsWithFilter(string filter)
        {
            _filter = filter;
        }

        public bool Match(string filepath)
        {
            return _filter.EndsWith(filepath, StringComparison.OrdinalIgnoreCase);
        }
    }

    internal class FuzzyFilter : IVideoIgnoreFilter
    {
        private readonly string[] filterParts;

        public FuzzyFilter(string filter)
        {
            this.filterParts = filter.Split('*');
        }

        public bool Match(string filepath)
        {
            var pos = 0;
            for (var i = 0; i < filterParts.Length; i++)
            {
                var part = filterParts[i];
                pos = filepath.IndexOf(part, pos, StringComparison.OrdinalIgnoreCase);
                if (pos == -1)
                {
                    return false;
                }
            }
            return true;
        }
    }

    internal class PassthroughFilter : IVideoIgnoreFilter
    {
        public bool Match(string filepath) => false;
    }
}