using System;
using System.Collections.Generic;
using System.Linq;
using MudBlazor;

namespace explainpowershell.frontend
{
    internal static class GenericTree
    {
        private const int MaxDepth = 256;

        /// <summary>
        /// Generates a MudBlazor-compatible tree from a flat collection.
        /// </summary>
        public static List<TreeItemData<T>> GenerateTree<T, K>(
            this IEnumerable<T> collection,
            Func<T, K> idSelector,
            Func<T, K> parentIdSelector,
            K rootId = default)
        {
            static K CanonicalizeKey(K key)
            {
                if (typeof(K) == typeof(string))
                {
                    var s = key as string;
                    return (K)(object)(s ?? string.Empty);
                }

                return key;
            }

            var comparer = EqualityComparer<K>.Default;
            var items = collection as IList<T> ?? collection.ToList();

            // Group children by parent id in one pass (O(n)).
            var childrenByParent = new Dictionary<K, List<T>>(comparer);
            foreach (var item in items)
            {
                var parentKey = CanonicalizeKey(parentIdSelector(item));
                if (!childrenByParent.TryGetValue(parentKey, out var list))
                {
                    list = new List<T>();
                    childrenByParent[parentKey] = list;
                }

                list.Add(item);
            }

            var rootKey = CanonicalizeKey(rootId);
            if (!childrenByParent.TryGetValue(rootKey, out var rootItems) || rootItems.Count == 0)
            {
                // Special-case: when rootId is null/empty string, treat both null and empty as root.
                if (typeof(K) == typeof(string) && string.IsNullOrEmpty(rootKey as string))
                {
                    if (childrenByParent.TryGetValue((K)(object)string.Empty, out var emptyRootItems))
                    {
                        rootItems = emptyRootItems;
                    }
                }

                if (rootItems is null || rootItems.Count == 0)
                {
                    return new List<TreeItemData<T>>();
                }
            }

            var nodes = new List<TreeItemData<T>>(rootItems.Count);

            var stack = new Stack<(TreeItemData<T> Node, K Id, int Depth, HashSet<K> Path)>();

            foreach (var item in rootItems)
            {
                var nodeId = CanonicalizeKey(idSelector(item));
                var node = new TreeItemData<T>
                {
                    Value = item,
                    Expanded = true
                };

                nodes.Add(node);
                stack.Push((node, nodeId, 1, new HashSet<K>(comparer) { nodeId }));
            }

            while (stack.Count > 0)
            {
                var (node, id, depth, path) = stack.Pop();

                if (depth >= MaxDepth)
                {
                    node.Children = null;
                    node.Expandable = false;
                    continue;
                }

                if (!childrenByParent.TryGetValue(id, out var children) || children.Count == 0)
                {
                    node.Children = null;
                    node.Expandable = false;
                    continue;
                }

                var childNodes = new List<TreeItemData<T>>(children.Count);

                foreach (var child in children)
                {
                    var childId = CanonicalizeKey(idSelector(child));
                    if (path.Contains(childId))
                    {
                        continue;
                    }

                    childNodes.Add(new TreeItemData<T>
                    {
                        Value = child,
                        Expanded = true
                    });
                }

                if (childNodes.Count == 0)
                {
                    node.Children = null;
                    node.Expandable = false;
                    continue;
                }

                node.Children = childNodes;
                node.Expandable = true;

                // Push in reverse so the first child is processed first.
                for (var i = childNodes.Count - 1; i >= 0; i--)
                {
                    var childNode = childNodes[i];
                    var childId = CanonicalizeKey(idSelector(childNode.Value));
                    var childPath = new HashSet<K>(path, comparer) { childId };
                    stack.Push((childNode, childId, depth + 1, childPath));
                }
            }

            return nodes;
        }
    }
}
