using System;
using System.Collections.Generic;
using System.Linq;
using MudBlazor;

namespace explainpowershell.frontend
{
    internal static class GenericTree
    {
        /// <summary>
        /// Generates a MudBlazor-compatible tree from a flat collection.
        /// </summary>
        public static List<TreeItemData<T>> GenerateTree<T, K>(
            this IEnumerable<T> collection,
            Func<T, K> idSelector,
            Func<T, K> parentIdSelector,
            K rootId = default)
        {
            var nodes = new List<TreeItemData<T>>();

            bool IsRoot(T item)
            {
                var parentId = parentIdSelector(item);

                // Special-case string keys: treat both null and empty as root.
                if (typeof(K) == typeof(string))
                {
                    var parentString = parentId as string;
                    var rootString = rootId as string;

                    if (string.IsNullOrEmpty(rootString))
                    {
                        return string.IsNullOrEmpty(parentString);
                    }

                    return string.Equals(parentString, rootString, StringComparison.Ordinal);
                }

                return EqualityComparer<K>.Default.Equals(parentId, rootId);
            }

            foreach (var item in collection.Where(IsRoot))
            {
                var children = collection.GenerateTree(idSelector, parentIdSelector, idSelector(item));

                nodes.Add(new TreeItemData<T>
                {
                    Value = item,
                    Expanded = true,
                    Expandable = children.Count > 0,
                    Children = children.Count == 0 ? null : children
                });
            }

            return nodes;
        }
    }
}
