using System;
using System.Collections.Generic;

namespace RecycleBin.DynamicProxy
{
   internal static class Helper
   {
      internal static IEnumerable<T> AsSingleEnumerable<T>(this T source)
      {
         yield return source;
      }

      internal static TValue TryGetValueOrCreate<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TValue> create)
      {
         TValue value;
         if (!dictionary.TryGetValue(key, out value))
         {
            value = create();
            dictionary.Add(key, value);
         }
         return value;
      }
   }
}
