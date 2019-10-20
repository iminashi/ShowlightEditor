using System;
using System.Collections.Generic;

namespace ShowlightEditor.Core.Extensions
{
    public static class LinqExtensions
    {
        /// <summary>
        /// Skips a specified number of elements at the end of a sequence.
        /// </summary>
        /// <typeparam name="T">Element type.</typeparam>
        /// <param name="enumerable"></param>
        /// <param name="skipCount">Number of elements to skip.</param>
        /// <returns></returns>
        /// <remarks>https://blogs.msdn.microsoft.com/ericwhite/2008/11/14/the-skiplast-extension-method/</remarks>
        public static IEnumerable<T> SkipLast<T>(this IEnumerable<T> enumerable, int skipCount)
        {
            if (skipCount < 0)
                throw new ArgumentException("Number of elements to skip cannot be negative.");

            return SkipLastImpl(enumerable, skipCount);

            // Implementation as local function
            IEnumerable<T> SkipLastImpl(IEnumerable<T> source, int count)
            {
                Queue<T> saveList = new Queue<T>(count + 1);
                foreach (T item in source)
                {
                    saveList.Enqueue(item);
                    if (count > 0)
                    {
                        --count;
                        continue;
                    }

                    yield return saveList.Dequeue();
                }
            }
        }

        /// <summary>
        /// Skips the last element at the end of a sequence.
        /// </summary>
        /// <typeparam name="T">Element type.</typeparam>
        /// <param name="enumerable"></param>
        /// <returns></returns>
        public static IEnumerable<T> SkipLast<T>(this IEnumerable<T> enumerable) => enumerable.SkipLast(1);
    }
}
