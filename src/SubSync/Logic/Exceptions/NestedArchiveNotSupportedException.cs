using System;

namespace SubSync
{
    public class NestedArchiveNotSupportedException : Exception
    {
        public NestedArchiveNotSupportedException(string filename)
            : base($"Downloaded archive, '{filename}' @red@contain another archive within it and cannot properly be extracted. Archive kept for manual labor.")
        {
        }
    }
}