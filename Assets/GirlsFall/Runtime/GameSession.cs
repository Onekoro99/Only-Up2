using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GirlsFall
{
    public static class GameSession
    {
        public const string GameplayScene = "CandyTown_EnclosedWorld";
        public const string LegacyGameplayScene = "GirlsFallGameplay";
        public const string TitleScene = "TitleScene";
        public static PlayerSaveState PendingState { get; private set; }
        public static bool NewGameRequested { get; private set; }
        public static bool IsTransitioning { get; private set; }
        public static void RequestNew() { PendingState = null; NewGameRequested = true; IsTransitioning = true; }
        public static void RequestContinue(PlayerSaveState state) { PendingState = state; NewGameRequested = false; IsTransitioning = true; }
        public static void Ready() { PendingState = null; NewGameRequested = false; IsTransitioning = false; }
        public static void Cancel() { PendingState = null; NewGameRequested = false; IsTransitioning = false; }
        public static void ReturningToTitle() { IsTransitioning = true; }
        public static void ExitApplication()
        {
            GameSettingsManager.Instance.Save(out _);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.ExitPlaymode();
#else
            Application.Quit();
#endif
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { PendingState = null; NewGameRequested = IsTransitioning = false; Time.timeScale = 1f; }
    }
}
