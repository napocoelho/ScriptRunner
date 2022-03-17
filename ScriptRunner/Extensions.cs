using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Collections.Concurrent;

namespace ScriptRunning
{
    public static class Extensions
    {
        public static bool Contains<T>(this SynchronizedCollection<T> collection, Predicate<T> predicate)
        {
            for (int idx = 0; idx < collection.Count; idx++)
            {
                try
                {
                    if (predicate(collection[idx]))
                    {
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    break;
                }
            }

            return false;
        }

        /// <summary>
        /// Removes the first result found.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="predicate"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static bool Remove<T>(this SynchronizedCollection<T> collection, Predicate<T> predicate, out T result)
        {
            for (int idx = 0; idx < collection.Count; idx++)
            {
                try
                {
                    T item = collection[idx];

                    if (predicate(item))
                    {
                        result = item;
                        collection.Remove(item);
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    break;
                }
            }

            result = default(T);
            return false;
        }

        public static string TakeLeft(this string text, int count)
        {
            StringBuilder builder = new StringBuilder("");

            for (int idx = 0; idx < text.Count() && idx < count; idx++)
            {
                builder.Append(text[idx]);
            }

            return builder.ToString();
        }
    }
}