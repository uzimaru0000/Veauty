using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Veauty.Patch;
using Veauty.VTree;
using Object = System.Object;

namespace Veauty
{
    public static class Diff
    {
        public static IPatch[] Calc(IVTree x, IVTree y)
        {
            var patches = new List<IPatch>();
            Helper(x, y, ref patches, 0);
            return patches.ToArray();
        }

        static IPatch PushPatch(ref List<IPatch> patches, IPatch patch)
        {
            patches.Add(patch);
            return patch;
        }

        static void Helper(IVTree x, IVTree y, ref List<IPatch> patches, int index)
        {
            // TODO: Implement `Equals` method on each node
            // if (x.Equals(y))
            // {
            //     return;
            // }

            if (x.GetNodeType() != y.GetNodeType())
            {
                if (x is BaseNode && y is BaseKeyedNode)
                {
                    switch (y)
                    {
                        case KeyedNode<Component> typedNode:
                            y = Dekey(typedNode);
                            break;
                        case KeyedNode keyedNode:
                            y = Dekey(keyedNode);
                            break;
                    }
                }
                else
                {
                    PushPatch(ref patches, new Redraw(index, y));
                    return;
                }
            }
            
            switch (y)
            {
                case BaseNode yNode:
                    if (x is BaseNode xNode)
                    {
                        DiffNodes(xNode, yNode, ref patches, index);
                    }

                    return;
                case BaseKeyedNode yKeyedVNode:
                    if (x is BaseKeyedNode xKeyedNode)
                    {
                        DiffKeyedNodes(xKeyedNode, yKeyedVNode, ref patches, index);
                    }

                    return;
                case Widget yWidget:
                    if (x is Widget xWidget)
                    {
                        DiffWidget(xWidget, yWidget, ref patches, index);
                    }

                    return;
                default:
                    throw new Exception("Invalid VTree type");
            }
        }

        static void DiffNodes(
            BaseNode x,
            BaseNode y,
            ref List<IPatch> patches,
            int index
        )
        {
            var attach = CheckComponentType(x, y, index);

            // Not TypedNode and not Equal tag name
            if (attach == null && x.tag != y.tag)
            {
                PushPatch(ref patches, new Redraw(index, y));
                return;
            }

            // TypedNode has a different type
            if (attach != null)
            {
                PushPatch(ref patches, attach);
            }

            CheckAttributes(x.attrs, y.attrs, ref patches, index);

            DiffKids(x, y, ref patches, index);
        }

        static void DiffKeyedNodes(
            BaseKeyedNode x,
            BaseKeyedNode y,
            ref List<IPatch> patches,
            int index
        )
        {
            if (x.tag != y.tag)
            {
                PushPatch(ref patches, new Redraw(index, y));
                return;
            }

            CheckAttributes(x.attrs, y.attrs, ref patches, index);

            DiffKeyedKids(x, y, ref patches, index);
        }

        static IPatch CheckComponentType(IVTree x, IVTree y, int index)
        {
            if (x is ITypedNode xNode && y is ITypedNode yNode && xNode.GetComponentType() != yNode.GetComponentType())
            {
                return new Attach(index, xNode.GetComponentType(), yNode.GetComponentType());
            }

            return null;
        }

        static void CheckAttributes(
            Attributes x,
            Attributes y,
            ref List<IPatch> patches,
            int index
        )
        {
            var attrsDiff = DiffAttributes(x, y);
            if (attrsDiff.Count > 0)
            {
                PushPatch(ref patches, new Attrs(index, attrsDiff));
            }
        }

        static Dictionary<string, IAttribute> DiffAttributes(
            Attributes x,
            Attributes y
        )
        {
            var xAttrs = x.attrs;
            var yAttrs = y.attrs;

            var diff = new Dictionary<string, IAttribute>();

            foreach (var kv in xAttrs)
            {
                var key = kv.Key;
                if (yAttrs.ContainsKey(key))
                {
                    if (!xAttrs[key].Equals(yAttrs[key]))
                    {
                        diff[key] = yAttrs[key];
                    }
                }
                else
                {
                    diff[key] = null;
                }
            }

            foreach (var kv in yAttrs)
            {
                var key = kv.Key;
                var value = kv.Value;
                if (!xAttrs.ContainsKey(key))
                {
                    diff[key] = value;
                }
            }

            return diff;
        }

        static void DiffKids(BaseNode xParent, BaseNode yParent, ref List<IPatch> patches, int index)
        {
            var xKids = xParent.kids;
            var yKids = yParent.kids;

            var xLen = xKids.Length;
            var yLen = yKids.Length;

            if (xLen > yLen)
            {
                PushPatch(ref patches, new RemoveLast(index, yLen, xLen - yLen));
            }
            else if (xLen < yLen)
            {
                PushPatch(ref patches, new Append(index, xLen, yKids));
            }

            var minLen = xLen < yLen ? xLen : yLen;
            for (var i = 0; i < minLen; i++)
            {
                index++;
                var xKid = xKids[i];
                Helper(xKid, yKids[i], ref patches, index);
                index += xKid.GetDescendantsCount();
            }
        }

        static void DiffKeyedKids(BaseKeyedNode xParent, BaseKeyedNode yParent, ref List<IPatch> patches, int rootIndex)
        {
            var localPatches = new List<IPatch>();

            var changes = new Dictionary<string, Entry>();
            var inserts = new List<Reorder.Insert>();

            var xKids = xParent.kids;
            var yKids = yParent.kids;
            var xLen = xKids.Length;
            var yLen = yKids.Length;

            var xIndex = 0;
            var yIndex = 0;

            var index = rootIndex;

            while (xIndex < xLen && yIndex < yLen)
            {
                var (xKey, xNode) = xKids[xIndex];
                var (yKey, yNode) = yKids[yIndex];

                var newMatch = false;
                var oldMatch = false;

                if (xKey == yKey)
                {
                    index++;
                    Helper(xNode, yNode, ref localPatches, index);
                    index += xNode.GetDescendantsCount();

                    xIndex++;
                    yIndex++;
                    continue;
                }

                var (xNextKey, xNextNode) = xIndex + 1 < xKids.Length ? xKids[xIndex + 1] : ("", null);
                var (yNextKey, yNextNode) = yIndex + 1 < yKids.Length ? yKids[yIndex + 1] : ("", null);

                oldMatch = yKey == xNextKey;
                newMatch = xKey == yNextKey;

                if (newMatch && oldMatch)
                {
                    index++;
                    Helper(xNode, yNextNode, ref localPatches, index);
                    InsertNode(ref changes, ref localPatches, xKey, yNode, yIndex, ref inserts);
                    index += xNode.GetDescendantsCount();

                    index++;
                    RemoveNode(ref changes, ref localPatches, xKey, xNextNode, index);
                    index += xNextNode.GetDescendantsCount();

                    xIndex += 2;
                    yIndex += 2;
                    continue;
                }

                if (newMatch)
                {
                    index++;
                    InsertNode(ref changes, ref localPatches, yKey, yNode, yIndex, ref inserts);
                    Helper(xNode, yNextNode, ref localPatches, index);
                    index += xNode.GetDescendantsCount();

                    xIndex += 1;
                    yIndex += 2;
                    continue;
                }

                if (oldMatch)
                {
                    index++;
                    RemoveNode(ref changes, ref localPatches, xKey, xNode, index);
                    index += xNode.GetDescendantsCount();

                    index++;
                    Helper(xNextNode, yNode, ref localPatches, index);
                    index += xNextNode.GetDescendantsCount();

                    xIndex += 2;
                    yIndex += 1;
                    continue;
                }

                if (xNextKey != "" && xNextNode != null && xNextKey == yNextKey)
                {
                    index++;
                    RemoveNode(ref changes, ref localPatches, xKey, xNode, index);
                    InsertNode(ref changes, ref localPatches, yKey, yNode, yIndex, ref inserts);
                    index += xNode.GetDescendantsCount();

                    index++;
                    Helper(xNextNode, yNextNode, ref localPatches, index);
                    index += xNextNode.GetDescendantsCount();

                    xIndex += 2;
                    yIndex += 2;
                    continue;
                }

                break;
            }

            while (xIndex < xLen)
            {
                index++;
                var (xKey, xNode) = xKids[xIndex];
                RemoveNode(ref changes, ref localPatches, xKey, xNode, index);
                index += xNode.GetDescendantsCount();
                xIndex++;
            }

            var endInserts = new List<Reorder.Insert>();
            while (yIndex < yLen)
            {
                var (yKey, yNode) = yKids[yIndex];
                InsertNode(ref changes, ref localPatches, yKey, yNode, -1, ref endInserts);
                yIndex++;
            }

            if (localPatches.Count > 0 || inserts.Count > 0 || endInserts.Count > 0)
            {
                PushPatch(
                    ref patches,
                    new Reorder(rootIndex, localPatches.ToArray(), inserts.ToArray(), endInserts.ToArray())
                );
            }
        }

        static void InsertNode(
            ref Dictionary<string, Entry> changes,
            ref List<IPatch> localPatches,
            string key,
            IVTree vTree,
            int yIndex,
            ref List<Reorder.Insert> inserts
        )
        {
            if (!changes.ContainsKey(key))
            {
                var newEntry = new Entry(Entry.Type.Insert, vTree, yIndex);
                inserts.Add(new Reorder.Insert {index = yIndex, entry = newEntry});
                changes[key] = newEntry;

                return;
            }

            var entry = changes[key];

            if (entry.tag == Entry.Type.Remove)
            {
                inserts.Add(new Reorder.Insert {index = yIndex, entry = entry});

                entry.tag = Entry.Type.Move;
                var subPatches = new List<IPatch>();
                Helper(entry.vTree, vTree, ref subPatches, entry.index);
                entry.index = yIndex;
                entry.data = new
                {
                    patches = subPatches,
                    entry = entry
                };

                return;
            }

            InsertNode(ref changes, ref localPatches, key + "UniVDOM", vTree, yIndex, ref inserts);
        }

        static void RemoveNode(
            ref Dictionary<string, Entry> changes,
            ref List<IPatch> localPatches,
            string key,
            IVTree vTree,
            int index
        )
        {
            if (!changes.ContainsKey(key))
            {
                var patch = PushPatch(ref localPatches, new Remove(index));

                changes[key] = new Entry(Entry.Type.Remove, vTree, index);
                changes[key].data = patch;

                return;
            }

            var entry = changes[key];

            if (entry.tag == Entry.Type.Insert)
            {
                entry.tag = Entry.Type.Remove;
                var subPatches = new List<IPatch>();
                Helper(vTree, entry.vTree, ref subPatches, index);

                PushPatch(ref localPatches, new Remove(index, subPatches.ToArray(), entry));
                return;
            }

            RemoveNode(ref changes, ref localPatches, key + "UniVDOM", vTree, index);
        }

        static void DiffWidget(Widget x, Widget y, ref List<IPatch> patches, int index)
        {
            var oldTree = x.Render();
            var newTree = y.Render();

            Helper(oldTree, newTree, ref patches, index);
        }

        static BaseNode Dekey(BaseKeyedNode keyedNode)
        {
            var kids = new IVTree[keyedNode.kids.Length];
            for (var i = 0; i < kids.Length; i++)
            {
                kids[i] = keyedNode.GetKids()[i];
            }
            
            var attrs = new List<IAttribute>();
            foreach (var kv in keyedNode.attrs.attrs)
            {
                attrs.Add(kv.Value);
            }

            if (keyedNode is ITypedNode typedNode)
            {
                var genericNodeType = typeof(Node<>).MakeGenericType(typedNode.GetComponentType());
                return (BaseNode) System.Activator.CreateInstance(genericNodeType, new object[] {keyedNode.tag, attrs.ToArray(), kids});
            }
            else
            {
                return new Node(keyedNode.tag, attrs.ToArray(), kids);
            }
        }
    }
}