using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Reflection;
using GirlsFall;
using OnlyUpPrototype;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GirlsFall.Editor
{
    [InitializeOnLoad]
    public static class GirlsFallTitleTests
    {
        private static IEnumerator routine;
        private static StringBuilder report;
        private static int failures;
        private static string testDirectory, originalSettings;
        private const string Flag = "GirlsFall.TitleTest.Pending";
        private const string QuitFlag = "GirlsFall.TitleTest.Quit";
        static GirlsFallTitleTests()
        {
            EditorApplication.playModeStateChanged += OnMode;
            EditorApplication.delayCall += AutoRun;
        }
        private static void AutoRun()
        {
            string marker = Path.Combine(Directory.GetParent(Application.dataPath).FullName, ".girlsfall-title-test-request");
            if (!File.Exists(marker)) return;
            if (File.Exists(Path.Combine(Directory.GetParent(Application.dataPath).FullName, ".girlsfall-title-layout-request")) ||
                File.Exists(Path.Combine(Directory.GetParent(Application.dataPath).FullName, ".girlsfall-title-theme-request")))
            { EditorApplication.delayCall += AutoRun; return; }
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            { EditorApplication.delayCall += AutoRun; return; }
            if (!AssetDatabase.LoadMainAssetAtPath(GirlsFallTitleBuilder.TitlePath)) { EditorApplication.delayCall += AutoRun; return; }
            foreach (EditorWindow window in Resources.FindObjectsOfTypeAll<EditorWindow>())
                if (window.GetType().Name == "TMP_PackageResourceImporterWindow") window.Close();
            if (SceneManager.GetActiveScene().path == GirlsFallTitleBuilder.TitlePath && SceneManager.GetActiveScene().isDirty)
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            File.Delete(marker); Run();
        }
        [MenuItem("Tools/Girls Fall/Test Title, Settings and Fall Saves")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Stop Play mode and save the current scene first.");
            EditorSceneManager.OpenScene(GirlsFallTitleBuilder.TitlePath);
            SessionState.SetBool(Flag, true); EditorApplication.EnterPlaymode();
        }
        private static void OnMode(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Flag, false))
            {
                SessionState.SetBool(Flag, false); report = new StringBuilder(); failures = 0;
                testDirectory = Path.GetFullPath("Library/GirlsFallTests/" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss"));
                Directory.CreateDirectory(testDirectory); SessionState.SetString("GirlsFall.TestDirectory", testDirectory);
                routine = Execute(); EditorApplication.update += Tick;
            }
            if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(QuitFlag, false))
            {
                SessionState.SetBool(QuitFlag, false);
                string text = SessionState.GetString("GirlsFall.TestReport", "");
                text += "PASS Title quit exits Editor Play mode\n";
                string directory = SessionState.GetString("GirlsFall.TestDirectory", "");
                string before = SessionState.GetString("GirlsFall.ProgressBeforeQuit", "");
                string path = Path.Combine(directory, "girls-fall-progress.json");
                bool unchanged = File.Exists(path) && File.ReadAllText(path) == before;
                text += (unchanged ? "PASS" : "FAIL") + " Title quit does not write progress\n";
                int count = SessionState.GetInt("GirlsFall.TestFailures", 0) + (unchanged ? 0 : 1);
                text += "Failures: " + count + "\n";
                File.WriteAllText(GirlsFallTitleBuilder.Root + "/Documentation/TitlePlaytestReport.txt", text);
                if (count == 0) Debug.Log("GIRLSFALL_TITLE_TEST_PASS\n" + text); else Debug.LogError("GIRLSFALL_TITLE_TEST_FAIL\n" + text);
            }
        }
        private static void Tick()
        {
            try { if (routine != null && routine.MoveNext()) return; }
            catch (Exception error) { failures++; report.AppendLine("EXCEPTION " + error); }
            EditorApplication.update -= Tick; routine = null;
            if (!SessionState.GetBool(QuitFlag, false))
            {
                report.AppendLine("Failures: " + failures);
                File.WriteAllText(GirlsFallTitleBuilder.Root + "/Documentation/TitlePlaytestReport.txt", report.ToString());
                Debug.LogError("GIRLSFALL_TITLE_TEST_ABORTED\n" + report); EditorApplication.ExitPlaymode();
            }
        }
        private static void Check(bool pass, string description) { report.AppendLine((pass ? "PASS " : "FAIL ") + description); if (!pass) failures++; }
        private static T Find<T>() where T : UnityEngine.Object { return UnityEngine.Object.FindFirstObjectByType<T>(FindObjectsInactive.Include); }
        private static IEnumerator WaitScene(string name)
        {
            double deadline = EditorApplication.timeSinceStartup + 20;
            while (SceneManager.GetActiveScene().name != name)
            { if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Scene " + name); yield return null; }
            for (int i = 0; i < 8; i++) yield return null;
        }
        private static IEnumerator Execute()
        {
            for (int i = 0; i < 10; i++) yield return null;
            SaveDataManager.UseStorageForTests(testDirectory);
            var settings = GameSettingsManager.Instance; originalSettings = JsonUtility.ToJson(settings.Current); settings.UseStorageForTests(testDirectory);
            TitleMenuController title = Find<TitleMenuController>(); title.RefreshContinue();
            Check(title && title.continueButton && title.newGameButton && title.optionsButton && title.quitButton, "Title has four bound uGUI buttons");
            Check(!title.continueButton.interactable, "Continue disabled with no save");
            Check(!File.Exists(SaveDataManager.Instance.SavePath), "Title does not create progress");
            Check(title.GetComponent<UnityEngine.UI.CanvasScaler>().referenceResolution == new Vector2(1920, 1080), "Canvas reference 1920x1080");
            Check(Find<UnityEngine.EventSystems.StandaloneInputModule>(), "Mouse/navigation EventSystem present");
            Check(title.newGameButton.GetComponentInChildren<TMP_Text>().font.HasCharacter('続'), "Japanese glyph in packaged font");

            title.optionsButton.onClick.Invoke();
            OptionsMenuController options = title.options;
            Check(options.gameObject.activeSelf, "Options button opens panel");
            options.masterSlider.value = 0.55f; options.bgmSlider.value = 0.22f; options.seSlider.value = 0.33f; options.sensitivitySlider.value = 7.25f;
            options.saveButton.onClick.Invoke();
            Check(Mathf.Abs(AudioListener.volume - 0.55f) < 0.01f, "Master slider applies");
            Check(Mathf.Abs(settings.BgmOutputVolume - 0.22f * 0.18f) < 0.001f && Mathf.Abs(settings.SeOutputVolume - 0.33f * 0.3f) < 0.001f, "BGM and SE independent output volumes");
            var settingsStore = new AtomicSaveFile(Path.Combine(testDirectory, "girls-fall-options.json"));
            bool settingsRead = settingsStore.TryRead(out string settingsJson, out _, out _);
            Check(settingsRead && Mathf.Abs(JsonUtility.FromJson<GameSettings>(settingsJson).mouseSensitivity - 7.25f) < 0.01f, "Options persisted independently");
            options.backButton.onClick.Invoke(); Check(!options.gameObject.activeSelf, "Options Back closes panel");
            title.newGameButton.onClick.Invoke(); Check(title.confirmation.activeSelf, "New game requires confirmation");
            title.confirmNo.onClick.Invoke(); Check(!title.confirmation.activeSelf && !SaveDataManager.Instance.HasSave, "No cancels new game without progress write");
            title.newGameButton.onClick.Invoke(); title.confirmYes.onClick.Invoke();
            Check(title.IsBusy, "Scene loading disables repeat activation");
            IEnumerator wait = WaitScene(GameSession.GameplayScene); while (wait.MoveNext()) yield return null;
            AutoSaveController auto = Find<AutoSaveController>(); PrototypePlayerMotor motor = Find<PrototypePlayerMotor>(); FallTracker falls = Find<FallTracker>();
            for (int i = 0; i < 30; i++) yield return null;
            Check(auto.CanSave && SaveDataManager.Instance.HasSave, "Confirmed new game creates initial slot in gameplay");
            Check(Mathf.Abs(settings.Current.se - 0.33f) < 0.01f, "New game retains options");
            Check(Mathf.Abs(Find<ThirdPersonOrbitCamera>().Sensitivity - 7.25f) < 0.01f, "Mouse sensitivity applied to gameplay camera");
            motor.enabled = false; PlayerSaveState ground = auto.Capture();
            ground.stage = 3; ground.pv = 125; ground.playSeconds = 321; auto.Restore(ground); auto.SaveNow();
            var gameMenu = Find<GameplayMenuController>(); gameMenu.ReturnToTitle();
            wait = WaitScene(GameSession.TitleScene); while (wait.MoveNext()) yield return null;
            title = Find<TitleMenuController>(); Check(title.continueButton.interactable, "Continue enabled after autosave");
            title.ContinueGame(); title.ContinueGame();
            wait = WaitScene(GameSession.GameplayScene); while (wait.MoveNext()) yield return null;
            auto = Find<AutoSaveController>(); motor = Find<PrototypePlayerMotor>(); falls = Find<FallTracker>(); motor.enabled = false;
            Check(Vector3.Distance(motor.transform.position, ground.position) < 0.15f, "Ground position restored after scene reload");
            Check(auto.Capture().stage == 3 && auto.Capture().pv == 125 && auto.Capture().playSeconds >= 321, "Stage/PV/time metadata restored");

            PlayerSaveState jumping = auto.Capture(); jumping.position = new Vector3(0, 10, 0); jumping.horizontalVelocity = new Vector3(0.5f, 0, 0);
            jumping.verticalVelocity = 4; jumping.grounded = false; jumping.airborne = true; jumping.falling = false; jumping.departureHeight = ground.position.y; jumping.peakHeight = 10;
            auto.Restore(jumping); auto.SaveNow(); Find<GameplayMenuController>().ReturnToTitle();
            wait = WaitScene(GameSession.TitleScene); while (wait.MoveNext()) yield return null;
            Find<TitleMenuController>().ContinueGame(); wait = WaitScene(GameSession.GameplayScene); while (wait.MoveNext()) yield return null;
            auto = Find<AutoSaveController>(); motor = Find<PrototypePlayerMotor>(); falls = Find<FallTracker>(); motor.enabled = false;
            Check(!motor.IsGrounded && motor.VerticalSpeed > 0 && motor.transform.position.y > 9.5f, "Jump resumes airborne with upward velocity");

            PlayerSaveState falling = auto.Capture(); falling.position = new Vector3(0, 7, 0); falling.verticalVelocity = -8; falling.falling = falling.airborne = true;
            falling.grounded = false; falling.peakHeight = 10; falling.departureHeight = ground.position.y; falling.horizontalVelocity = new Vector3(0.8f, 0, 0);
            auto.Restore(falling); auto.SaveNow(); Find<GameplayMenuController>().ReturnToTitle();
            wait = WaitScene(GameSession.TitleScene); while (wait.MoveNext()) yield return null;
            Find<TitleMenuController>().ContinueGame(); wait = WaitScene(GameSession.GameplayScene); while (wait.MoveNext()) yield return null;
            auto = Find<AutoSaveController>(); motor = Find<PrototypePlayerMotor>(); falls = Find<FallTracker>(); motor.enabled = false;
            Check(!motor.IsGrounded && motor.VerticalSpeed < 0 && falls.IsFalling, "Fall remains airborne and descending after reload");
            Check(motor.transform.position.y <= 7.01f && motor.transform.position.y > 5.5f && Mathf.Abs(falls.PeakHeight - 10) < 0.01f, "Fall height and start height restored without high-point rollback");
            PlayerSaveState velocitySnapshot = auto.LoadedState;
            Check(velocitySnapshot != null && Mathf.Abs(velocitySnapshot.horizontalVelocity.x - 0.8f) < 0.01f, "Horizontal momentum persisted through scene reload");
            motor.RestoreRuntimeState(motor.transform.position, motor.transform.rotation, velocitySnapshot.horizontalVelocity, motor.VerticalSpeed, false);
            Check(Mathf.Abs(motor.HorizontalVelocity.x - 0.8f) < 0.01f, "Motor restores exact horizontal velocity before normal air-control braking");
            motor.Teleport(new Vector3(0, 0.12f, 0), Quaternion.identity); falls.ResetTracking(); motor.enabled = true;
            for (int i = 0; i < 100; i++) yield return null;
            Check(motor.IsGrounded, "Restored player can land"); motor.enabled = false; auto.SaveNow();
            Check(SaveDataManager.Instance.TryLoad(out PlayerSaveState landed, out _) && landed.grounded && !landed.falling, "Landing persists grounded state");
            PrototypeGoal goal = Find<PrototypeGoal>(); motor.Teleport(goal.transform.position, Quaternion.identity); goal.CheckPlayer();
            Check(SaveDataManager.Instance.TryLoad(out PlayerSaveState completed, out _) && completed.stageCompleted, "Stage completion autosaved");

            // Storage failure and crash recovery use a separate test-only store.
            string crashDir = Path.Combine(testDirectory, "crash-simulation"); var store = new SaveDataManager(crashDir);
            store.Save(ground, false); store.Save(falling, true); store.Flush();
            Check(new SaveDataManager(crashDir).TryLoad(out PlayerSaveState durable, out _) && durable.falling && durable.verticalVelocity < 0, "Critical fall snapshot survives new manager/process simulation");
            Check(durable != null && durable.position.y == 7, "Old queued grounded snapshot cannot overwrite newer fall");
            File.WriteAllText(store.SavePath + ".pending", "{ interrupted write");
            Check(new SaveDataManager(crashDir).TryLoad(out durable, out _) && durable.falling, "Torn pending write retains last durable falling state");
            falling.position.y = 6;
            File.WriteAllText(store.SavePath + ".pending", AtomicSaveFile.Pack(1000000, JsonUtility.ToJson(falling)));
            Check(new SaveDataManager(crashDir).TryLoad(out durable, out _) && durable.position.y == 6, "Complete newer transaction selected after interrupted replacement");
            File.Delete(store.SavePath + ".pending"); File.WriteAllText(store.SavePath, "corrupt");
            Check(!new SaveDataManager(crashDir).TryLoad(out _, out string corruptError) && !string.IsNullOrEmpty(corruptError), "Corruption returns error without crashing");

            Find<GameplayMenuController>().ReturnToTitle(); wait = WaitScene(GameSession.TitleScene); while (wait.MoveNext()) yield return null;
            title = Find<TitleMenuController>();
            byte[] validProgress = File.ReadAllBytes(SaveDataManager.Instance.SavePath);
            File.WriteAllText(SaveDataManager.Instance.SavePath, "corrupt"); title.ContinueGame();
            Check(title.errorPanel.activeSelf && !title.IsBusy && SceneManager.GetActiveScene().name == GameSession.TitleScene, "Continue displays corruption error and stays on title");
            File.WriteAllBytes(SaveDataManager.Instance.SavePath, validProgress); title.errorClose.onClick.Invoke(); title.RefreshContinue();
            title.newGameButton.onClick.Invoke(); title.confirmNo.onClick.Invoke();
            Check(Convert.ToBase64String(File.ReadAllBytes(SaveDataManager.Instance.SavePath)) == Convert.ToBase64String(validProgress), "Declining overwrite preserves existing save bytes");
            title.newGameButton.onClick.Invoke(); title.confirmYes.onClick.Invoke();
            wait = WaitScene(GameSession.GameplayScene); while (wait.MoveNext()) yield return null;
            auto = Find<AutoSaveController>(); motor = Find<PrototypePlayerMotor>(); motor.enabled = false;
            Check(auto.Capture().stage == 1 && auto.Capture().pv == 0 && !auto.StageCompleted, "Confirmed new game resets progress metadata");
            Check(Mathf.Abs(settings.Current.mouseSensitivity - 7.25f) < 0.01f, "Confirmed overwrite preserves settings");
            Find<GameplayMenuController>().ReturnToTitle(); wait = WaitScene(GameSession.TitleScene); while (wait.MoveNext()) yield return null;
            title = Find<TitleMenuController>();
            var canvas = title.GetComponent<RectTransform>(); Vector2 oldSize = canvas.sizeDelta;
            Check(title.GetComponent<ResponsiveTitleLayout>().design != null, "Responsive artwork/menu design root bound");
            Check(Find<UnityEngine.UI.RawImage>().GetComponent<UnityEngine.UI.AspectRatioFitter>().aspectMode == UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent, "Artwork preserves aspect ratio with fit layout");
            var artwork = Find<UnityEngine.UI.RawImage>().texture;
            Check(Mathf.Abs((float)artwork.width / artwork.height - 1672f / 941f) < 0.001f, "Artwork import retains original pixel aspect ratio");
            foreach (Vector2Int requested in new[] { new Vector2Int(1280, 720), new Vector2Int(1024, 768), new Vector2Int(2560, 1080) })
            {
                Action restoreView;
                bool configured = ConfigureTestGameView(requested.x, requested.y, out restoreView);
                for (int frame = 0; frame < 20; frame++) yield return null;
                Canvas.ForceUpdateCanvases();
                Check(configured && Screen.width == requested.x && Screen.height == requested.y, "Actual GameView resolution " + Screen.width + "x" + Screen.height + " matches " + requested.x + "x" + requested.y);
                bool inside = true;
                foreach (var button in new[] { title.continueButton, title.newGameButton, title.optionsButton, title.quitButton })
                {
                    Vector3[] corners = new Vector3[4]; button.GetComponent<RectTransform>().GetWorldCorners(corners);
                    foreach (Vector3 corner in corners)
                    {
                        Vector2 point = RectTransformUtility.WorldToScreenPoint(null, corner);
                        inside &= point.x >= -1 && point.x <= Screen.width + 1 && point.y >= -1 && point.y <= Screen.height + 1;
                    }
                }
                Check(inside, "All menu buttons inside screen after resolution request " + requested.x + "x" + requested.y + " (Editor actual " + Screen.width + "x" + Screen.height + ")");
                restoreView?.Invoke();
            }
            JsonUtility.FromJsonOverwrite(originalSettings, settings.Current); settings.Apply(false);
            SessionState.SetString("GirlsFall.ProgressBeforeQuit", File.ReadAllText(SaveDataManager.Instance.SavePath));
            SessionState.SetString("GirlsFall.TestReport", report.ToString()); SessionState.SetInt("GirlsFall.TestFailures", failures); SessionState.SetBool(QuitFlag, true);
            title.quitButton.onClick.Invoke();
        }

        private static bool ConfigureTestGameView(int width, int height, out Action restore)
        {
            restore = null;
            try
            {
                Assembly assembly = typeof(UnityEditor.Editor).Assembly;
                Type sizesType = assembly.GetType("UnityEditor.GameViewSizes");
                Type singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
                object sizes = singleton.GetProperty("instance").GetValue(null);
                MethodInfo getGroup = sizesType.GetMethod("GetGroup");
                Type groupParameter = getGroup.GetParameters()[0].ParameterType;
                object groupKey = groupParameter.IsEnum ? Enum.ToObject(groupParameter, 1) : (object)1;
                object group = getGroup.Invoke(sizes, new[] { groupKey });
                Type groupType = group.GetType();
                int total = (int)groupType.GetMethod("GetTotalCount").Invoke(group, null);
                int custom = (int)groupType.GetMethod("GetCustomCount").Invoke(group, null);
                Type sizeType = assembly.GetType("UnityEditor.GameViewSize");
                Type modeType = assembly.GetType("UnityEditor.GameViewSizeType");
                object size = Activator.CreateInstance(sizeType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null,
                    new object[] { Enum.Parse(modeType, "FixedResolution"), width, height, "GirlsFall temporary test" }, null);
                groupType.GetMethod("AddCustomSize").Invoke(group, new[] { size });
                Type viewType = assembly.GetType("UnityEditor.GameView");
                EditorWindow view = EditorWindow.GetWindow(viewType);
                PropertyInfo selected = viewType.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                int previous = (int)selected.GetValue(view);
                selected.SetValue(view, total); view.Repaint();
                restore = () => { selected.SetValue(view, previous); groupType.GetMethod("RemoveCustomSize").Invoke(group, new object[] { custom }); view.Repaint(); };
                return true;
            }
            catch (Exception error) { report.AppendLine("INFO GameView resize unavailable: " + error.GetType().Name); return false; }
        }
    }
}
