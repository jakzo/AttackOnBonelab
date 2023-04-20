using MelonLoader;
using UnityEngine;
using HarmonyLib;
using SLZ.Bonelab;
using SLZ.Rig;
using SLZ.Props;
using SLZ.VRMK;
using SLZ.Marrow.Warehouse;
using System.Collections.Generic;
using System.Linq;
using System;

namespace AttackOnBonelab
{
    public static class Utils
    {
        public static void ForEachChild(
            Transform transform,
            Action<GameObject> action
        )
        {
            for (var i = 0; i < transform.childCount; i++)
                action(transform.GetChild(i).gameObject);
        }

        public static IEnumerable<T> GetNestedComponents<T>(GameObject go)
            where T : Component
        {
            foreach (var component in go.GetComponents<T>())
                yield return component;
            var tr = go.transform;
            for (var i = 0; i < tr.childCount; i++)
            {
                var components = GetNestedComponents<T>(
                    tr.GetChild(i).gameObject
                );
                foreach (var component in components)
                    yield return component;
            }
        }
    }

    public static class Log
    {
        public static void Debug(string message)
        {
#if DEBUG
            MelonLogger.Msg($"[debug] {message}");
#endif
        }
    }
}
