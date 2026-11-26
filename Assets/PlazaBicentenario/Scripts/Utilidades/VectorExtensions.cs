// Utilidades vectoriales y de componentes locales para SupervivenciaEnLaPaz.
using System.Collections.Generic;
using UnityEngine;

namespace SupervivenciaEnLaPaz.Utilidades
{
    public static class VectorExtensions
    {
        public static Vector3 WithXAndZ(this Vector3 v) => new(v.x, 0f, v.z);

        public static Vector2 ToVec2WithXAndZ(this Vector3 v) => new(v.x, v.z);

        public static Vector3 ToVec3XZ(this Vector2 v) => new(v.x, 0f, v.y);

        public static T GetOrAddComponent<T>(this GameObject go) where T : Component
        {
            if (go.TryGetComponent(out T existing))
                return existing;
            return go.AddComponent<T>();
        }

        public static bool AddIfNotPresent<T>(this IList<T> list, T item)
        {
            if (list == null || list.Contains(item))
                return false;
            list.Add(item);
            return true;
        }

        public static void RemoveNullOrDestroyed<T>(this List<T> list) where T : Object
        {
            if (list == null || list.Count == 0)
                return;

            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i] == null)
                    list.RemoveAt(i);
            }
        }
    }
}
