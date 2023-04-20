using System;
using MelonLoader;
using UnityEngine.SceneManagement;
using HarmonyLib;
using SLZ.Marrow.Warehouse;
using SLZ.Marrow.SceneStreaming;
using SLZ.Rig;

namespace AttackOnBonelab
{
    static class GameState
    {
        public static LevelCrate PrevLevel;
        public static LevelCrate CurrentLevel;
        public static LevelCrate NextLevel;
        public static bool IsLoading
        {
            get => !CurrentLevel;
        }

        public static RigManager RigManager;

        public static event Action<LevelCrate> OnLoad;
        public static event Action<LevelCrate> OnLevelStart;

        private static Scene _loadingScene;

        private static void SafeInvoke(
            string name,
            Action<LevelCrate> action,
            LevelCrate level
        )
        {
            try
            {
                action?.Invoke(level);
            }
            catch (Exception ex)
            {
                MelonLogger.Error(
                    $"Failed to execute {name} event: {ex.ToString()}"
                );
            }
        }

        private static void WaitForLoadFinished()
        {
            if (_loadingScene.isLoaded)
                return;
            MelonEvents.OnUpdate.Unsubscribe(WaitForLoadFinished);

            CurrentLevel =
                NextLevel ?? SceneStreamer.Session.Level ?? CurrentLevel;
            NextLevel = null;
            Log.Debug($"OnLevelStart {CurrentLevel?.Title}");
            SafeInvoke("OnLevelStart", OnLevelStart, CurrentLevel);
        }

        [HarmonyPatch(typeof(BasicTrackingRig), nameof(BasicTrackingRig.Awake))]
        class BasicTrackingRig_Awake_Patch
        {
            [HarmonyPrefix()]
            internal static void Prefix(BasicTrackingRig __instance)
            {
                Log.Debug("BasicTrackingRig_Awake_Patch");
                _loadingScene = __instance.gameObject.scene;
                if (CurrentLevel)
                    PrevLevel = CurrentLevel;
                CurrentLevel = null;
                NextLevel = SceneStreamer.Session.Level;
                RigManager = null;
                MelonEvents.OnUpdate.Subscribe(WaitForLoadFinished);
                Log.Debug($"OnLoad {NextLevel?.Title}");
                SafeInvoke("OnLoad", OnLoad, NextLevel);
            }
        }

        [HarmonyPatch(typeof(RigManager), nameof(RigManager.Awake))]
        class RigManager_Awake_Patch
        {
            [HarmonyPrefix()]
            internal static void Prefix(RigManager __instance)
            {
                if (!RigManager)
                    RigManager = __instance;
            }
        }
    }
}
