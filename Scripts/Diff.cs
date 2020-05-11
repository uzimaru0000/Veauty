using System;
using System.Collections;
using System.Collections.Generic;

namespace VDOM
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

            if (x.GetType() != y.GetType())
            {
                if (x.GetType() == VTreeType.Node && y.GetType() == VTreeType.KeyedNode)
                {
                    y = Dekey((KeyedVNode)y);
                }
                else
                {
                    PushPatch(ref patches, new Redraw(index, y));
                    return;
                }
            }
            else
            {
                switch (y)
                {
                    case VText vText:
                        if (x.GetType() == VTreeType.Text && ((VText)x).text != vText.text)
                        {
                            PushPatch(ref patches, new Text(index, vText.text));
                            return;
                        }
                        return;
                    case VNode vNode:
                        if (x.GetType() == VTreeType.Node)
                        {
                            DiffNodes((VNode)x, vNode, ref patches, index);
                        }
                        return;
                    case KeyedVNode keyedVNode:
                        if (x.GetType() == VTreeType.KeyedNode)
                        {
                            DiffKeyedNodes((KeyedVNode)x, keyedVNode, ref patches, index);
                        }
                        return;
                    case Widget widget:
                        if (x.GetType() == VTreeType.Widget)
                        {
                            DiffWidget((Widget) x, widget, ref patches, index);
                            return;
                        }
                        return;
                    default:
                        throw new Exception("Invalid VTree type");
                }
            }
        }

        static void DiffNodes(
            VNode x,
            VNode y,
            ref List<IPatch> patches,
            int index
        )
        {
            if (x.tag != y.tag)
            {
                PushPatch(ref patches, new Redraw(index, y));
                return;
            }

            var attrsDiff = DiffAttrs(x.attrs, y.attrs);
            if ((attrsDiff.attrs.ContainsKey(AttributeType.Event) && attrsDiff.attrs[AttributeType.Event].Count > 0) ||
                (attrsDiff.attrs.ContainsKey(AttributeType.Props) && attrsDiff.attrs[AttributeType.Props].Count > 0)
            )
            {
                PushPatch(ref patches, new Attrs(index, attrsDiff));
            }

            DiffKids(x, y, ref patches, index);
        }

        static void DiffKeyedNodes(
            KeyedVNode x,
            KeyedVNode y,
            ref List<IPatch> patches,
            int index
        )
        {
            if (x.tag != y.tag)
            {
                PushPatch(ref patches, new Redraw(index, y));
                return;
            }

            var attrsDiff = DiffAttrs(x.attrs, y.attrs);
            if ((attrsDiff.attrs.ContainsKey(AttributeType.Event) && attrsDiff.attrs[AttributeType.Event].Count > 0) ||
                (attrsDiff.attrs.ContainsKey(AttributeType.Props) && attrsDiff.attrs[AttributeType.Props].Count > 0)
            )
            {
                PushPatch(ref patches, new Attrs(index, attrsDiff));
            }

            DiffKeyedKids(x, y, ref patches, index);
        }

        static Attributes DiffAttrs(Attributes x, Attributes y)
        {
            var diff = new Attributes(new IAttribute[0]);

            foreach (var kv in x.attrs)
            {
                diff.attrs[kv.Key] = DiffProp(kv.Value, y.attrs.ContainsKey(kv.Key) ? y.attrs[kv.Key] : new Dictionary<string, IAttribute>());
            }

            foreach (var kv in y.attrs)
            {
                if (!x.attrs.ContainsKey(kv.Key))
                {
                    diff.attrs[kv.Key] = DiffProp(new Dictionary<string, IAttribute>(), kv.Value);
                }
            }

            return diff;
        }

        static Dictionary<string, IAttribute> DiffProp(
            Dictionary<string, IAttribute> x,
            Dictionary<string, IAttribute> y
        )
        {
            var diff = new Dictionary<string, IAttribute>();

            foreach (var kv in x)
            {
                if (y.ContainsKey(kv.Key) && kv.Value.GetType() == y[kv.Key].GetType())
                {
                    switch (kv.Value.GetType())
                    {
                        case AttributeType.Event:
                            diff[kv.Value.GetKey()] = y[kv.Value.GetKey()];
                            diff[$"remove_{kv.Value.GetKey()}"] = kv.Value;
                            continue;
                        case AttributeType.Props:
                            if (!kv.Value.Equals(y[kv.Value.GetKey()]))
                            {
                                diff[kv.Value.GetKey()] = y[kv.Value.GetKey()];
                            }
                            continue;
                    }
                }
                else
                {
                    diff[kv.Key] = null;
                }
            }

            foreach (var kv in y)
            {
                if (!x.ContainsKey(kv.Key))
                {
                    diff[kv.Key] = kv.Value;
                }
            }

            return diff;
        }

        static void DiffKids(VNode xParent, VNode yParent, ref List<IPatch> patches, int index)
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

        static void DiffKeyedKids(KeyedVNode xParent, KeyedVNode yParent, ref List<IPatch> patches, int rootIndex)
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
                inserts.Add(new Reorder.Insert { index = yIndex, entry = newEntry });
                changes[key] = newEntry;

                return;
            }

            var entry = changes[key];

            if (entry.tag == Entry.Type.Remove)
            {
                inserts.Add(new Reorder.Insert { index = yIndex, entry = entry });

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

        static VNode Dekey(KeyedVNode keyedNode)
        {
            var kids = new IVTree[keyedNode.kids.Length];
            for (var i = 0; i < keyedNode.kids.Length; i++)
            {
                kids[i] = keyedNode.kids[i].Item2;
            }

            var attrs = new List<IAttribute>();
            foreach (var keyValuePair in keyedNode.attrs.attrs)
            {
                foreach (var kv in keyValuePair.Value)
                {
                    attrs.Add(kv.Value);
                }

            }

            return new VNode(keyedNode.tag, attrs.ToArray(), kids);
        }

    }
}
