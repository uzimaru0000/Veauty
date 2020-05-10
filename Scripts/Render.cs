using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UI = UnityEngine.UI;

namespace VDOM
{
    public static class Renderer
    {
        public static GameObject Render(IVTree vTree)
        {
            switch (vTree)
            {
                case VText t:
                    return CreateTextNode(t.text);
                case VNode vNode:
                    return CreateVNode(vNode);
                case KeyedVNode vNode:
                    return CreateVNode(vNode);
                case Widget widget:
                    return widget.Init();
                default:
                    return null;
            }
        }

        private static GameObject CreateGameObject(string name)
        {
            var go = new GameObject(name);
            go.AddComponent<RectTransform>();
            
            return go;
        }

        private static GameObject CreateTextNode(string text)
        {
            var go = CreateGameObject(text); 
            
            var textComponent = go.AddComponent<UI.Text>();
            textComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            textComponent.text = text;
            textComponent.alignment = TextAnchor.MiddleCenter;

            return go;
        }

        private static GameObject CreateVNode(VNode vNode)
        {
            var go = CreateGameObject(vNode.tag);
            
            ApplyAttrs(go, vNode.attrs);

            foreach (var kid in vNode.kids)
            {
                AppendChild(go, Render(kid));
            }
            
            return go;
        }

        private static GameObject CreateVNode(KeyedVNode vNode)
        {
            var go = CreateGameObject(vNode.tag);
            
            ApplyAttrs(go, vNode.attrs);

            foreach (var kid in vNode.kids)
            {
                AppendChild(go, Render(kid.Item2));
            }

            return go;
        }

        private static void AppendChild(GameObject parent, GameObject kid)
        {
            var parentRectTransform = parent.GetComponent<RectTransform>();
            var kidRectTransform = kid.GetComponent<RectTransform>();

            if (parentRectTransform && kidRectTransform)
            {
                kidRectTransform.sizeDelta = parentRectTransform.sizeDelta;
                kidRectTransform.anchoredPosition = new Vector2(0.5f, 0.5f);
            }
            
            kid.transform.SetParent(parent.transform);
        }

        private static void ApplyAttrs(GameObject go, Attributes attrs)
        {
            foreach (var attr in attrs.attrs)
            {
                switch (attr.Key)
                {
                    case AttributeType.Event:
                        // applyEvent(go, attr.Value);
                        break;
                    case AttributeType.Props:
                        // applyAttrs(go, attr.Value);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
        
    }
}
