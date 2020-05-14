using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UI = UnityEngine.UI;
using Veauty.VTree;

namespace Veauty
{
    public static class Renderer
    {
        public static GameObject Render(IVTree vTree)
        {
            switch (vTree)
            {
                case VText t:
                    return CreateTextNode(t.text);
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

        private static GameObject CreateTextNode(string text)
        {
            var go = CreateGameObject(text); 
            
            var textComponent = go.AddComponent<UI.Text>();
            textComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            textComponent.text = text;
            textComponent.alignment = TextAnchor.MiddleCenter;
            textComponent.color = Color.black;

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
}
