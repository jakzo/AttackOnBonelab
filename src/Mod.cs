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
using SLZ;
using SLZ.Interaction;

namespace AttackOnBonelab
{
    public class Mod : MelonMod
    {
        private static GameObject PREFAB_RETICLE;

        public override void OnInitializeMelon()
        {
            PREFAB_RETICLE = Geometry.CreatePrefabCube(
                "aob_reticle",
                Color.magenta,
                -0.1f,
                0.1f,
                -0.1f,
                0.1f,
                -0.1f,
                0.1f,
                Resources
                    .FindObjectsOfTypeAll<Shader>()
                    .First(shader => shader.name == "SLZ/Highlighter")
            );

            GameState.OnLevelStart += OnLevelStart;
        }

        private void OnLevelStart(LevelCrate level)
        {
            var hands = new Hand[]
            {
                GameState.RigManager?.physicsRig?.leftHand,
                GameState.RigManager?.physicsRig?.rightHand,
            };
            foreach (var hand in hands)
            {
                if (!hand)
                    continue;
                var grapplingHook =
                    hand.gameObject.AddComponent<GrapplingHook>();
                grapplingHook.Hand = hand;
            }
        }
    }
}
