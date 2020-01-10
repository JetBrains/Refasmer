using System;
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

        public static IEnumerable<T> FlattenTree<T>(this IEnumerable<T> nodes, Func<T, IEnumerable<T>> nested)
        {
            foreach (var node in nodes)
            {
                yield return node;

                var nestedNodes = nested(node);

                if (nestedNodes == null) 
                    continue;
                
                foreach (var nestedNode in FlattenTree(nestedNodes, nested))
                    yield return nestedNode;
            }
        }
        
    }
}