using System;
using System.Collections.Generic;

namespace JetBrains.Refasmer
{
    public static class NetStandardSubstitution
    {
        public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> pair, out TKey key, out TValue value)
        {
            key = pair.Key;
            value = pair.Value;
        }

        public static TValue GetValueOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key) where TKey : notnull
        {
            return dictionary.GetValueOrDefault(key, default);
        }

        public static TValue GetValueOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue) where TKey : notnull
        {
            if(dictionary == null)
                throw new ArgumentNullException(nameof(dictionary));
            TValue obj;
            return !dictionary.TryGetValue(key, out obj) ? defaultValue : obj;
        }
    }
}