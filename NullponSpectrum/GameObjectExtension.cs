using System;
using UnityEngine;

namespace NullponSpectrum
{
    public static class GameObjectExtension
    {
        public static string GetFullPath(this GameObject go, bool withSceneName = true)
        {
            string text = go.name;
            Transform parent = go.transform.parent;
            while (parent != null)
            {
                text = parent.name + "/" + text;
                parent = parent.parent;
            }
            if (withSceneName)
            {
                text = "(" + go.scene.name + ")/" + text;
            }
            return text;
        }
    }
}
