using System;
using System.Collections;
using System.IO;
using GirlsFall;
using OnlyUpPrototype;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GirlsFall.Editor
{
    [InitializeOnLoad]
    public static class GirlsFallRestartTests
    {
        private const string Active = "GirlsFall.Restart.Active";
        private const string Case = "GirlsFall.Restart.Case";
        private const string Loading = "GirlsFall.Restart.Loading";
        private const string DirectoryKey = "GirlsFall.Restart.Directory";
        private const string Expected = "GirlsFall.Restart.Expected";
        private const string ReportKey = "GirlsFall.Restart.Report";
        private const string Failures = "GirlsFall.Restart.Failures";
        private static IEnumerator routine;
        static GirlsFallRestartTests()
        {
            EditorApplication.playModeStateChanged += OnMode;
            EditorApplication.delayCall += AutoRun;
        }
        private static void AutoRun()
        {
            string marker = Path.GetFullPath(".girlsfall-restart-test-request");
            if (!File.Exists(marker)) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating ||
                File.Exists(".girlsfall-title-test-request")) { EditorApplication.delayCall += AutoRun; return; }
            File.Delete(marker); Run();
        }
        [MenuItem("Tools/Girls Fall/Test Editor Quit and Resume")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (SceneManager.GetActiveScene().isDirty && SceneManager.GetActiveScene().path != GirlsFallTitleBuilder.TitlePath) return;
            if (SceneManager.GetActiveScene().isDirty) EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            SessionState.SetBool(Active, true); SessionState.SetBool(Loading, false); SessionState.SetInt(Case, 0);
            SessionState.SetString(ReportKey, ""); SessionState.SetInt(Failures, 0);
            string directory = Path.GetFullPath("Library/GirlsFallRestartTests/" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss"));
            Directory.CreateDirectory(directory); SessionState.SetString(DirectoryKey, directory);
            StartPlay();
        }
        private static void StartPlay()
        {
            EditorSceneManager.OpenScene(GirlsFallTitleBuilder.TitlePath);
            EditorApplication.EnterPlaymode();
        }
        private static void OnMode(PlayModeStateChange mode)
        {
            if (!SessionState.GetBool(Active, false)) return;
            if (mode == PlayModeStateChange.EnteredPlayMode)
            {
                routine = Execute(); EditorApplication.update += Tick;
            }
            if (mode == PlayModeStateChange.EnteredEditMode)
            {
                int index = SessionState.GetInt(Case, 0);
                if (index >= 3)
                {
                    SessionState.SetBool(Active, false);
                    string text = SessionState.GetString(ReportKey, "") + "Failures: " + SessionState.GetInt(Failures, 0) + "\n";
                    File.WriteAllText(GirlsFallTitleBuilder.Root + "/Documentation/RestartPlaytestReport.txt", text);
                    if (SessionState.GetInt(Failures, 0) == 0) Debug.Log("GIRLSFALL_RESTART_TEST_PASS\n" + text); else Debug.LogError("GIRLSFALL_RESTART_TEST_FAIL\n" + text);
                    return;
                }
                var store = new SaveDataManager(SessionState.GetString(DirectoryKey, ""));
                bool loaded = store.TryLoad(out PlayerSaveState actual, out _);
                PlayerSaveState expected = JsonUtility.FromJson<PlayerSaveState>(SessionState.GetString(Expected, "{}"));
                Check(loaded && Vector3.Distance(actual.position, expected.position) < 0.01f && Mathf.Abs(actual.verticalVelocity - expected.verticalVelocity) < 0.01f,
                    "Editor quit saves latest runtime position/velocity for case " + index);
                SessionState.SetBool(Loading, true);
                EditorApplication.delayCall += StartPlay;
            }
        }
        private static void Tick()
        {
            try { if (routine != null && routine.MoveNext()) return; }
            catch (Exception error) { Check(false, "Exception " + error); SessionState.SetInt(Case, 3); EditorApplication.ExitPlaymode(); }
            EditorApplication.update -= Tick; routine = null;
        }
        private static void Check(bool pass, string description)
        {
            SessionState.SetString(ReportKey, SessionState.GetString(ReportKey, "") + (pass ? "PASS " : "FAIL ") + description + "\n");
            if (!pass) SessionState.SetInt(Failures, SessionState.GetInt(Failures, 0) + 1);
        }
        private static IEnumerator Execute()
        {
            for (int i = 0; i < 8; i++) yield return null;
            string directory = SessionState.GetString(DirectoryKey, "");
            SaveDataManager.UseStorageForTests(directory); GameSettingsManager.Instance.UseStorageForTests(directory);
            bool loading = SessionState.GetBool(Loading, false);
            int index = SessionState.GetInt(Case, 0);
            if (!loading)
            {
                GameSettingsManager.Instance.Current.mouseSensitivity = 6.25f;
                GameSettingsManager.Instance.Current.master = 0.42f;
                GameSettingsManager.Instance.Apply(false);
            }
            else Check(Mathf.Abs(GameSettingsManager.Instance.Current.mouseSensitivity - 6.25f) < 0.01f &&
                Mathf.Abs(GameSettingsManager.Instance.Current.master - 0.42f) < 0.01f, "Options restored across Editor stop/new Play for case " + index);
            if (loading) UnityEngine.Object.FindFirstObjectByType<TitleMenuController>().ContinueGame();
            else { GameSession.RequestNew(); SceneManager.LoadSceneAsync(GameSession.GameplayScene); }
            double deadline = EditorApplication.timeSinceStartup + 20;
            while (SceneManager.GetActiveScene().name != GameSession.GameplayScene)
            { if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException(); yield return null; }
            for (int i = 0; i < 8; i++) yield return null;
            AutoSaveController auto = UnityEngine.Object.FindFirstObjectByType<AutoSaveController>();
            PrototypePlayerMotor motor = auto.GetComponent<PrototypePlayerMotor>(); FallTracker falls = auto.GetComponent<FallTracker>();
            motor.enabled = falls.enabled = false;
            if (loading)
            {
                PlayerSaveState expected = JsonUtility.FromJson<PlayerSaveState>(SessionState.GetString(Expected, "{}"));
                PlayerSaveState restored = auto.LoadedState;
                Check(restored != null && Vector3.Distance(restored.position, expected.position) < 0.01f &&
                    Mathf.Abs(restored.verticalVelocity - expected.verticalVelocity) < 0.01f && restored.falling == expected.falling && restored.grounded == expected.grounded,
                    "New Play session restores " + (index == 0 ? "grounded" : index == 1 ? "jumping" : "falling") + " snapshot exactly");
                Check(index == 0 ? motor.IsGrounded : (!motor.IsGrounded && (index == 1 ? motor.VerticalSpeed > 0 : motor.VerticalSpeed < 0 && falls.IsFalling)),
                    "Controller continues correct physical state for case " + index);
                index++; SessionState.SetInt(Case, index);
                if (index >= 3) { GameSession.ReturningToTitle(); EditorApplication.ExitPlaymode(); yield break; }
            }
            PlayerSaveState target = auto.Capture();
            target.stage = 2; target.pv = 88; target.playSeconds = 400 + index;
            target.position = index == 0 ? new Vector3(0, 0.095812f, 0) : new Vector3(0, index == 1 ? 10 : 7, 0);
            target.rotation = Quaternion.Euler(0, 45, 0);
            target.horizontalVelocity = index == 0 ? Vector3.zero : new Vector3(0.8f, 0, 0);
            target.verticalVelocity = index == 0 ? -2f : index == 1 ? 4f : -8f;
            target.grounded = index == 0; target.airborne = index != 0; target.falling = index == 2;
            target.departureHeight = 0.095812f; target.peakHeight = index == 0 ? 0.095812f : 10;
            auto.Restore(target);
            // Do not manually save: OnApplicationQuit must persist this new unsaved runtime state.
            SessionState.SetString(Expected, JsonUtility.ToJson(auto.Capture()));
            EditorApplication.ExitPlaymode();
        }
    }
}
