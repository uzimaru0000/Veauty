using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEditor.Graphs;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UI = UnityEngine.UI;
using Veauty.VTree;

namespace Veauty 
{
    public static class Patch
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
                        patch.SetGameObject(gameObjectNode);
                        
                        var subPatches = ((Reorder)patch).patches;
                        if (subPatches.Length > 0)
                        {
                            AddGameObjectNodesHelper(gameObjectNode, vTree, ref subPatches, 0, low, high);
                        }

                        break;
                    }
                    case Remove remove:
                    {
                        patch.SetGameObject(gameObjectNode);

                        if (remove.patches != null && remove.entry != null)
                        {
                            ((Remove) patch).entry.data = gameObjectNode;
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
                case Text text:
                {
                    var textComponent = go.GetComponent<UI.Text>();
                    textComponent.text = text.text;
                    return go;
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

                    ((Remove) patch).entry.data = Helper(go, remove.patches);
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

        private static IEnumerator Delay(int num, System.Action func)
        {
            yield return new WaitWhile(() => --num > 0);
            func();
        }
    }

    public enum PatchType
    {   
        Redraw,
        Text,
        Attrs,
        RemoveLast,
        Append,
        Remove,
        Reorder,
        Attach
    }
    
    public interface IPatch
    {
        PatchType GetType();
        GameObject GetGameObject();
        void SetGameObject(in GameObject go);
        int GetIndex();
    }

    public class Entry
    {
        public enum Type
        {
            Move,
            Remove,
            Insert
        }
        
        public Type tag;
        public IVTree vTree;
        public int index;
        public object data;

        public Entry(Type tag, IVTree vTree, int index)
        {
            this.tag = tag;
            this.vTree = vTree;
            this.index = index;
        }
    }

    public struct Redraw : IPatch
    {
        public int index;
        public IVTree vTree;
        public GameObject gameObject;

        public Redraw(int index, IVTree vTree)
        {
            this.index = index;
            this.vTree = vTree;
            this.gameObject = null;
        }

        public PatchType GetType() => PatchType.Redraw;
        public GameObject GetGameObject() => this.gameObject;
        public void SetGameObject(in GameObject go) => this.gameObject = go;

        public int GetIndex() => this.index;

    }

    public struct Text : IPatch
    {
        public int index;
        public string text;
        public GameObject gameObject;

        public Text(int index, string text)
        {
            this.index = index;
            this.text = text;
            this.gameObject = null;
        }

        public PatchType GetType() => PatchType.Text;
        public GameObject GetGameObject() => this.gameObject;
        public void SetGameObject(in GameObject go) => this.gameObject = go;
        public int GetIndex() => this.index;
    }

    public struct Attrs : IPatch
    {

        public int index;
        public GameObject gameObject;

        public Dictionary<string, IAttribute> attrs;

        public Attrs(int index, Dictionary<string, IAttribute> attrs)
        {
            this.index = index;
            this.attrs = attrs;
            this.gameObject = null;
        }

        public PatchType GetType() => PatchType.Attrs;
        public GameObject GetGameObject() => this.gameObject;
        public void SetGameObject(in GameObject go) => this.gameObject = go;
        public int GetIndex() => this.index;
    }

    public struct RemoveLast : IPatch
    {

        public int index;
        public GameObject gameObject;

        public int length;
        public int diff;

        public RemoveLast(int index, int length, int diff)
        {
            this.index = index;
            this.length = length;
            this.diff = diff;
            this.gameObject = null;
        }

        public PatchType GetType() => PatchType.RemoveLast;
        public GameObject GetGameObject() => this.gameObject;
        public void SetGameObject(in GameObject go) => this.gameObject = go;
        public int GetIndex() => this.index;
    }

    public struct Append : IPatch
    {

        public int index;
        public GameObject gameObject;

        public int length;
        public IVTree[] kids;

        public Append(int index, int length, IVTree[] kids)
        {
            this.index = index;
            this.length = length;
            this.kids = kids;
            this.gameObject = null;
        }

        public PatchType GetType() => PatchType.Append;
        public GameObject GetGameObject() => this.gameObject;
        public void SetGameObject(in GameObject go) => this.gameObject = go;
        public int GetIndex() => this.index;
    }

    public struct Reorder : IPatch
    {

        public int index;
        public IPatch[] patches;
        public Insert[] inserts;
        public Insert[] endInserts;
        public GameObject gameObject;

        public Reorder(int index, IPatch[] patches, Insert[] inserts, Insert[] endInserts)
        {
            this.index = index;
            this.patches = patches;
            this.inserts = inserts;
            this.endInserts = endInserts;
            this.gameObject = null;
        }

        public PatchType GetType() => PatchType.Reorder;
        public GameObject GetGameObject() => this.gameObject;

        public void SetGameObject(in GameObject go) => this.gameObject = go;   
        public int GetIndex() => this.index;

        public struct Insert
        {
            public int index;
            public Entry entry;
        }
    }

    public struct Remove : IPatch
    {
        public int index;
        public GameObject gameObject;

        public IPatch[] patches;

        public Entry entry;

        public Remove(int index, IPatch[] patches = null, Entry entry = null)
        {
            this.index = index;
            this.patches = patches;
            this.entry = entry;
            this.gameObject = null;
        }

        public PatchType GetType() => PatchType.Remove;
        public GameObject GetGameObject() => this.gameObject;
        public void SetGameObject(in GameObject go) => this.gameObject = go;
        public int GetIndex() => this.index;
    }

    public struct Attach : IPatch
    {
        private GameObject gameObject;
        private readonly int index;
        public readonly System.Type oldComponent;
        public readonly System.Type newComponent;

        public Attach(int index, System.Type oldComponent, System.Type newComponent)
        {
            this.index = index;
            this.oldComponent = oldComponent;
            this.newComponent = newComponent;
            this.gameObject = null;
        }
        
        public PatchType GetType() => PatchType.Attach;

        public GameObject GetGameObject() => this.gameObject;

        public void SetGameObject(in GameObject go) => this.gameObject = go;

        public int GetIndex() => this.index;
    }
}
