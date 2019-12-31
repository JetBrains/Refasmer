using System.Collections.Generic;

namespace JetBrains.Refasmer
{
    public static class CollectionUtils
    {
        public static void RemoveRange<T>(this ICollection<T> collection, IEnumerable<T> items)
        {
            foreach (var item in items)
                collection.Remove(item);
        }
        
        public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> items)
        {
            foreach (var item in items)
                collection.Add(item);
        }
        
    }
}