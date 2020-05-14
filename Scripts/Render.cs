using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Veauty.Patch;
using Veauty.VTree;

namespace Veauty
{
    public static class Renderer
    {
        public static GameObject Render(IVTree vTree)
        {
            switch (vTree)
            {
                case BaseNode vNode:
                    return CreateNode(vNode);
                case BaseKeyedNode vNode:
                    return CreateNode(vNode);
                case Widget widget:
                    return widget.Init(Render(widget.Render())); 
                default:
                    return null;
            }
        }

        private static GameObject CreateGameObject(string name)
        {
            var go = new GameObject(name);
            var rectTransform = go.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;
            
            return go;
        }

        private static GameObject CreateNode(BaseNode vNode)
        {
            var go = CreateGameObject(vNode.tag);

            if (vNode is ITypedNode node)
            {
                go.AddComponent(node.GetComponentType());
            }
            
            ApplyAttrs(go, vNode.attrs);

            foreach (var kid in vNode.kids)
            {
                AppendChild(go, Render(kid));
            }
            
            return go;
        }

        private static GameObject CreateNode(BaseKeyedNode vNode)
        {
            var go = CreateGameObject(vNode.tag);
            
            if (vNode is ITypedNode node)
            {
                go.AddComponent(node.GetComponentType());
            }
            
            ApplyAttrs(go, vNode.attrs);

            foreach (var kid in vNode.kids)
            {
                AppendChild(go, Render(kid.Item2));
            }

            return go;
        }

        private static void AppendChild(GameObject parent, GameObject kid)
        {
            kid.transform.SetParent(parent.transform, false);
        }

        private static void ApplyAttrs(GameObject go, Attributes attrs)
        {
            foreach (var attr in attrs.attrs)
            {
                attr.Value.Apply(go);
            }
        }

    }
    
    public static class Patching
    {
        public static GameObject Apply(GameObject rootGameObject, IVTree oldVTree, IPatch[] patches)
        {
            if (patches.Length == 0)
            {
                return rootGameObject;
            }
            
            AddGameObjectNodes(rootGameObject, oldVTree, ref patches);
            
            return Helper(rootGameObject, patches);
        }

        private static void AddGameObjectNodes(in GameObject gameObjectNode, IVTree vTree, ref IPatch[] patches)
        {
            AddGameObjectNodesHelper(gameObjectNode, vTree, ref patches, 0, 0, vTree.GetDescendantsCount());
        }

        private static int AddGameObjectNodesHelper(in GameObject gameObjectNode, IVTree vTree, ref IPatch[] patches, int i,
            int low, int high)
        {
            var patch = patches[i];
            var index = patch.GetIndex();

            while (index == low)
            {
                switch (patch)
                {
                    case Reorder reorder:
                    {
                        reorder.SetGameObject(gameObjectNode);
                        
                        var subPatches = reorder.patches;
                        if (subPatches.Length > 0)
                        {
                            AddGameObjectNodesHelper(gameObjectNode, vTree, ref subPatches, 0, low, high);
                        }

                        break;
                    }
                    case Remove remove:
                    {
                        remove.SetGameObject(gameObjectNode);

                        if (remove.patches != null && remove.entry != null)
                        {
                            remove.entry.data = gameObjectNode;
                            var subPatches = remove.patches;
                            if (subPatches.Length > 0)
                            {
                                AddGameObjectNodesHelper(gameObjectNode, vTree, ref subPatches, 0, low, high);
                            }
                        }
                        
                        break;
                    }
                    default:
                        patch.SetGameObject(gameObjectNode);
                        break;
                }

                i++;

                if (i < patches.Length)
                {
                    patch = patches[i];
                    index = patch.GetIndex();
                    if (index > high)
                    {
                        return i;
                    }
                }
                else
                {
                    return i;
                }
            }
            
            if (vTree is IParent parent)
            {
                var kids = parent.GetKids();
                var children = gameObjectNode.transform;
                for (var j = 0; j < kids.Length; j++)
                {
                    low++;
                    var kid = kids[j];
                    var nextLow = low + kid.GetDescendantsCount();
                    if (low <= index && index <= nextLow)
                    {
                        i = AddGameObjectNodesHelper(children.GetChild(j).gameObject, kid, ref patches, i, low, nextLow);
                        
                        if (i < patches.Length)
                        {
                            patch = patches[i];
                            index = patch.GetIndex();
                            if (index > high)
                            {
                                return i;
                            }
                        }
                        else
                        {
                            return i;
                        }
                    }

                    low = nextLow;
                }
            }

            return i;
        }

        private static GameObject Helper(GameObject rootGameObject, IPatch[] patches)
        {
            foreach (var patch in patches)
            {
                var localGameObject = patch.GetGameObject();
                var newGameObject = ApplyPatch(localGameObject, patch);
                if (localGameObject == rootGameObject)
                {
                    rootGameObject = newGameObject;
                }
            }
            
            return rootGameObject;
        }

        private static GameObject ApplyPatch(GameObject go, IPatch patch)
        {
            switch (patch)
            {
                case Redraw redraw:
                {
                    return ApplyPatchRedraw(go, redraw.vTree);
                }
                case Attrs attrs:
                {
                    return ApplyAttrs(go, attrs.attrs);
                }
                case RemoveLast removeLast:
                {
                    for (var i = 0; i < removeLast.diff; i++)
                    {
                        var child = go.transform.GetChild(removeLast.length);
                        GameObject.Destroy(child);
                    }
                    return go;
                }
                case Append append:
                {
                    var kids = append.kids;
                    var i = append.length;
                    for (; i < kids.Length; i++)
                    {
                        var node = Renderer.Render(kids[i]);
                        node.transform.SetParent(go.transform);
                    }
                    return go;
                }
                case Remove remove:
                {
                    if (remove.entry == null && remove.patches == null)
                    {
                        go.transform.SetParent(null);
                        return go;
                    }

                    if (remove.entry.index != -1)
                    {
                        go.transform.SetParent(null);
                    }

                    remove.entry.data = Helper(go, remove.patches);
                    return go;
                }
                case Reorder reorder:
                {
                    return ApplyPatchReorder(go, reorder);
                }
                case Attach attach:
                {
                    return ApplyPatchAttach(go, attach);
                }
            }
            return go;
        }

        private static GameObject ApplyPatchRedraw(GameObject go, IVTree vTree)
        {
            var parent = go.transform.parent;
            var newNode = Renderer.Render(vTree);

            if (parent && newNode != go)
            {
                GameObject.Destroy(go);
                newNode.transform.SetParent(parent, false);
            }
            
            return newNode;
        }

        private static GameObject ApplyAttrs(GameObject go, Dictionary<string, IAttribute> attrs)
        {
            foreach (var attr in attrs)
            {
                attr.Value.Apply(go);
            }
            
            return go;
        }

        private static GameObject ApplyPatchReorder(GameObject go, Reorder patch)
        {
            var frag = ApplyPatchReorderEndInsertsHelper(patch.endInserts, patch);
            
            go = Helper(go, patch.patches);

            foreach (var insert in patch.inserts)
            {
                var entry = insert.entry;
                var node = entry.tag == Entry.Type.Move ? entry.data as GameObject : Renderer.Render(entry.vTree);
                node.transform.SetParent(go.transform);
                node.transform.SetSiblingIndex(insert.index); 
            }

            if (frag != null)
            {
                foreach (var child in frag)
                {
                    child.transform.SetParent(go.transform);
                }
            }

            return go;
        }

        private static GameObject[] ApplyPatchReorderEndInsertsHelper(Reorder.Insert[] endInserts, Reorder patch)
        {
            if (endInserts.Length == 0)
            {
                return null;
            }
            
            var frag = new GameObject[endInserts.Length];
            for (var i = 0; i < endInserts.Length; i++)
            {
                var insert = endInserts[i];
                var entry = insert.entry;
                frag[i] = entry.tag == Entry.Type.Move ? entry.data as GameObject : Renderer.Render(entry.vTree);
            }

            return frag;
        }

        private static GameObject ApplyPatchAttach(GameObject go, Attach attach)
        {
            var old = go.GetComponent(attach.oldComponent) as MonoBehaviour;
            GameObject.Destroy(old);

            go.AddComponent(attach.newComponent);
            
            return go;
        }
    }
}
