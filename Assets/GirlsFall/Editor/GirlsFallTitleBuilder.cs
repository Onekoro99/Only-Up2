using System;
using System.IO;
using System.Linq;
using GirlsFall;
using OnlyUpPrototype;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace GirlsFall.Editor
{
    [InitializeOnLoad]
    public static class GirlsFallTitleBuilder
    {
        public const string Root = "Assets/GirlsFall";
        public const string TitlePath = Root + "/Scenes/TitleScene.unity";
        public const string GamePath = Root + "/Scenes/GirlsFallGameplay.unity";
        private static TMP_FontAsset font;
        private static Sprite rounded;
        private static readonly Color Ink = new Color(0.39f, 0.23f, 0.34f);
        private static readonly Color Pink = new Color(1f, 0.90f, 0.94f);
        private static readonly Color Blue = new Color(0.84f, 0.95f, 1f);
        private static string Marker => Path.Combine(Directory.GetParent(Application.dataPath).FullName, ".girlsfall-title-build-request");
        static GirlsFallTitleBuilder() { EditorApplication.delayCall += AutoBuild; }
        private static void AutoBuild()
        {
            string themeMarker = Path.Combine(Directory.GetParent(Application.dataPath).FullName, ".girlsfall-title-theme-request");
            if (File.Exists(themeMarker))
            {
                if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
                { EditorApplication.delayCall += AutoBuild; return; }
                File.Delete(themeMarker); PolishExistingTitle();
            }
            if (!File.Exists(Marker)) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            { EditorApplication.delayCall += AutoBuild; return; }
            if (SceneManager.GetActiveScene().isDirty) { Debug.LogWarning("Girls Fall: save the current scene, then use Tools/Girls Fall/Create Title and Save Integration."); return; }
            File.Delete(Marker);
            try { Build(); } catch (Exception error) { Debug.LogException(error); }
        }

        [MenuItem("Tools/Girls Fall/Polish Title Controls")]
        public static void PolishExistingTitle()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (SceneManager.GetActiveScene().path != TitlePath)
            {
                if (SceneManager.GetActiveScene().isDirty) return;
                EditorSceneManager.OpenScene(TitlePath);
            }
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Root + "/Fonts/GirlsFallJapaneseSDF.asset");
            var options = UnityEngine.Object.FindFirstObjectByType<OptionsMenuController>(FindObjectsInactive.Include);
            foreach (Slider slider in new[] { options.masterSlider, options.bgmSlider, options.seSlider, options.sensitivitySlider })
            {
                Outline outline = slider.handleRect.GetComponent<Outline>();
                if (!outline) outline = slider.handleRect.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(0.80f, 0.40f, 0.58f); outline.effectDistance = new Vector2(2, -2);
            }
            options.fullscreen.graphic.color = new Color(0.78f, 0.38f, 0.56f);
            if (!options.resolution.transform.Find("DropdownIndicator"))
                Text("DropdownIndicator", options.resolution.transform as RectTransform, "▼", new Vector2(230, 0), new Vector2(40, 50), 24);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene()); EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }

        [MenuItem("Tools/Girls Fall/Create Title and Save Integration")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Save the current scene and stop Play mode.");
            if (AssetDatabase.LoadMainAssetAtPath(TitlePath) || AssetDatabase.LoadMainAssetAtPath(GamePath)) throw new InvalidOperationException("Refusing to overwrite existing Girls Fall scenes.");
            Folder(Root + "/Scenes"); Folder(Root + "/UI"); Folder(Root + "/Fonts"); Folder(Root + "/Documentation");
            if (!TMP_Settings.instance)
            {
                string package = Path.GetFullPath("Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage");
                if (!File.Exists(package))
                    package = Directory.GetFiles("Library/PackageCache", "TMP Essential Resources.unitypackage", SearchOption.AllDirectories).First();
                AssetDatabase.ImportPackage(package, false);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                EditorApplication.delayCall += Build;
                return;
            }
            CreateRoundedSprite();
            Font source = AssetDatabase.LoadAssetAtPath<Font>(Root + "/Fonts/NotoSansCJKjp-Regular.otf");
            if (!source) throw new InvalidOperationException("Japanese font not imported.");
            font = TMP_FontAsset.CreateFontAsset(source, 48, 5, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
            font.name = "GirlsFall Japanese SDF";
            AssetDatabase.CreateAsset(font, Root + "/Fonts/GirlsFallJapaneseSDF.asset");
            foreach (Texture2D atlas in font.atlasTextures) if (atlas) AssetDatabase.AddObjectToAsset(atlas, font);
            AssetDatabase.AddObjectToAsset(font.material, font);
            string characters = "続きからはじめオプションゲーム終了現在のセーブデータが削除されます。最初始？はいえ読み込中…設定保存できません破損可能性対応不正空容量アクセス権確認ください戻るマスター音BGMSEフルスクリーンウィンドウ解像度感度日本語落下冒険一時停止タイトルへ再開キャンセル変更しました無効設定を保存しました。ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789% ×：／（）!?-.";
            font.TryAddCharacters(new string(characters.Distinct().ToArray()), out _);
            EditorUtility.SetDirty(font); AssetDatabase.SaveAssetIfDirty(font);

            if (!AssetDatabase.CopyAsset("Assets/OnlyUpPrototype/Scenes/OnlyUp_Prototype.unity", GamePath)) throw new IOException("Cannot copy gameplay scene.");
            Scene game = EditorSceneManager.OpenScene(GamePath);
            GameObject player = GameObject.Find("model_chara");
            AutoSaveController auto = player.AddComponent<AutoSaveController>();
            var goal = UnityEngine.Object.FindFirstObjectByType<PrototypeGoal>();
            var serialized = new SerializedObject(auto); serialized.FindProperty("goal").objectReferenceValue = goal; serialized.ApplyModifiedPropertiesWithoutUndo();
            BuildPause(auto);
            EditorSceneManager.MarkSceneDirty(game); EditorSceneManager.SaveScene(game);

            Scene title = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject cameraObject = new GameObject("TitleCamera");
            Camera camera = cameraObject.AddComponent<Camera>(); camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(1f, 0.94f, 0.97f); cameraObject.AddComponent<AudioListener>();
            RectTransform canvas = CanvasRoot("TitleCanvas");
            RectTransform design = Design(canvas);
            string artworkPath = Root + "/UI/TitleArtwork.png";
            if (!File.Exists(artworkPath)) File.Copy("Assets/background/キャンディの空から少女がダイブ.png", artworkPath);
            AssetDatabase.ImportAsset(artworkPath);
            var artworkImporter = (TextureImporter)AssetImporter.GetAtPath(artworkPath);
            artworkImporter.npotScale = TextureImporterNPOTScale.None; artworkImporter.maxTextureSize = 4096;
            artworkImporter.mipmapEnabled = false; artworkImporter.textureCompression = TextureImporterCompression.Uncompressed; artworkImporter.SaveAndReimport();
            Texture2D artwork = AssetDatabase.LoadAssetAtPath<Texture2D>(artworkPath);
            if (!artwork) throw new InvalidOperationException("Title artwork missing.");
            GameObject background = new GameObject("GirlsFallArtwork", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter));
            background.transform.SetParent(design, false);
            RawImage image = background.GetComponent<RawImage>(); image.texture = artwork; image.raycastTarget = false;
            AspectRatioFitter fitter = background.GetComponent<AspectRatioFitter>(); fitter.aspectRatio = (float)artwork.width / artwork.height; fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;

            TitleMenuController menu = canvas.gameObject.AddComponent<TitleMenuController>();
            RectTransform panel = Panel("CandyMenu", design, new Vector2(590f, -355f), new Vector2(450f, 302f), new Color(1f, 0.98f, 1f, 0.9f));
            menu.continueButton = Button("続きから", panel, new Vector2(0, 96), new Vector2(386, 52), Pink);
            menu.newGameButton = Button("はじめから", panel, new Vector2(0, 32), new Vector2(386, 52), Color.white);
            menu.optionsButton = Button("オプション", panel, new Vector2(0, -32), new Vector2(386, 52), Blue);
            menu.quitButton = Button("ゲーム終了", panel, new Vector2(0, -96), new Vector2(386, 52), Color.white);
            menu.statusText = Text("LoadStatus", panel, "", new Vector2(0, -137), new Vector2(410, 28), 19);

            menu.options = BuildOptions(canvas);
            RectTransform confirm = Overlay("NewGameConfirmation", canvas);
            menu.confirmation = confirm.gameObject;
            RectTransform confirmPanel = Panel("ConfirmationCard", Design(confirm), Vector2.zero, new Vector2(970, 360), new Color(1f, 0.98f, 1f));
            Text("Question", confirmPanel, "現在のセーブデータが削除されます。\n最初から始めますか？", new Vector2(0, 55), new Vector2(870, 150), 35);
            menu.confirmYes = Button("はい", confirmPanel, new Vector2(-215, -90), new Vector2(350, 68), Pink);
            menu.confirmNo = Button("いいえ", confirmPanel, new Vector2(215, -90), new Vector2(350, 68), Blue);
            confirm.gameObject.SetActive(false);

            RectTransform error = Overlay("SaveError", canvas); menu.errorPanel = error.gameObject;
            RectTransform errorCard = Panel("ErrorCard", Design(error), Vector2.zero, new Vector2(1000, 430), Color.white);
            Text("ErrorHeading", errorCard, "お知らせ", new Vector2(0, 140), new Vector2(850, 60), 38);
            menu.errorText = Text("ErrorMessage", errorCard, "", new Vector2(0, 30), new Vector2(850, 170), 28);
            menu.errorClose = Button("戻る", errorCard, new Vector2(0, -125), new Vector2(360, 64), Blue);
            error.gameObject.SetActive(false);
            EditorSceneManager.MarkSceneDirty(title); EditorSceneManager.SaveScene(title, TitlePath);

            var oldScenes = EditorBuildSettings.scenes.Where(s => s.path != TitlePath && s.path != GamePath).ToList();
            oldScenes.Insert(0, new EditorBuildSettingsScene(GamePath, true));
            oldScenes.Insert(0, new EditorBuildSettingsScene(TitlePath, true));
            EditorBuildSettings.scenes = oldScenes.ToArray();
            AssetDatabase.SaveAssets();
            File.WriteAllText(Root + "/Documentation/BuildReport.txt", "GIRLSFALL_BUILD_OK\nTitleScene and GirlsFallGameplay created.\nExisting scenes and progress retained.\n");
            AssetDatabase.ImportAsset(Root + "/Documentation/BuildReport.txt");
            Debug.Log("GIRLSFALL_BUILD_OK");
        }

        private static OptionsMenuController BuildOptions(RectTransform canvas)
        {
            RectTransform overlay = Overlay("OptionsOverlay", canvas);
            var control = overlay.gameObject.AddComponent<OptionsMenuController>();
            RectTransform card = Panel("OptionsCard", Design(overlay), Vector2.zero, new Vector2(1120, 940), new Color(1f, 0.98f, 1f));
            Text("Heading", card, "オプション", new Vector2(0, 385), new Vector2(900, 70), 46);
            control.masterSlider = Slider("マスター音量", card, 270, 0, 1, out control.masterValue);
            control.bgmSlider = Slider("BGM音量", card, 180, 0, 1, out control.bgmValue);
            control.seSlider = Slider("SE音量", card, 90, 0, 1, out control.seValue);
            Text("DisplayLabel", card, "フルスクリーン", new Vector2(-310, -20), new Vector2(320, 55), 28);
            var toggleObject = DefaultControls.CreateToggle(new DefaultControls.Resources { standard = rounded, background = rounded, checkmark = rounded });
            toggleObject.name = "FullscreenToggle"; toggleObject.transform.SetParent(card, false);
            Rect(toggleObject.GetComponent<RectTransform>(), new Vector2(220, -20), new Vector2(340, 55));
            var legacy = toggleObject.GetComponentInChildren<UnityEngine.UI.Text>(); if (legacy) { legacy.text = ""; }
            control.fullscreen = toggleObject.GetComponent<Toggle>();
            Rect(control.fullscreen.targetGraphic.rectTransform, new Vector2(-135, 0), new Vector2(40, 40));
            control.fullscreen.targetGraphic.color = new Color(0.93f, 0.77f, 0.87f);
            control.fullscreen.graphic.color = new Color(0.78f, 0.38f, 0.56f);
            Text("FullscreenText", toggleObject.transform as RectTransform, "有効", Vector2.zero, new Vector2(200, 50), 26);
            Text("ResolutionLabel", card, "解像度", new Vector2(-310, -115), new Vector2(320, 60), 28);
            var dropdownObject = TMP_DefaultControls.CreateDropdown(new TMP_DefaultControls.Resources { standard = rounded, background = rounded });
            dropdownObject.name = "ResolutionDropdown"; dropdownObject.transform.SetParent(card, false);
            Rect(dropdownObject.GetComponent<RectTransform>(), new Vector2(155, -115), new Vector2(530, 60));
            control.resolution = dropdownObject.GetComponent<TMP_Dropdown>();
            foreach (TMP_Text label in dropdownObject.GetComponentsInChildren<TMP_Text>(true)) { label.font = font; label.fontSize = 26; label.color = Ink; }
            Text("DropdownIndicator", dropdownObject.transform as RectTransform, "▼", new Vector2(230, 0), new Vector2(40, 50), 24);
            control.sensitivitySlider = Slider("マウス感度", card, -220, 0.2f, 10f, out control.sensitivityValue);
            control.feedback = Text("SettingsFeedback", card, "", new Vector2(0, -300), new Vector2(1000, 60), 24);
            control.defaultsButton = Button("初期設定に戻す", card, new Vector2(-330, -385), new Vector2(300, 65), Color.white);
            control.saveButton = Button("設定を保存", card, new Vector2(0, -385), new Vector2(290, 65), Pink);
            control.backButton = Button("戻る", card, new Vector2(325, -385), new Vector2(280, 65), Blue);
            overlay.gameObject.SetActive(false); return control;
        }

        private static void BuildPause(AutoSaveController autosave)
        {
            RectTransform canvas = CanvasRoot("GirlsFallPauseCanvas");
            var menu = canvas.gameObject.AddComponent<GameplayMenuController>(); menu.autosave = autosave;
            RectTransform overlay = Overlay("PauseOverlay", canvas); menu.panel = overlay.gameObject;
            RectTransform card = Panel("PauseCard", Design(overlay), Vector2.zero, new Vector2(850, 650), new Color(1f, 0.98f, 1f));
            Text("PauseTitle", card, "一時停止", new Vector2(0, 235), new Vector2(700, 60), 44);
            menu.resumeButton = Button("ゲームに戻る", card, new Vector2(0, 120), new Vector2(580, 70), Pink);
            menu.titleButton = Button("タイトルへ戻る", card, new Vector2(0, 20), new Vector2(580, 70), Blue);
            menu.quitButton = Button("ゲーム終了", card, new Vector2(0, -80), new Vector2(580, 70), Color.white);
            menu.message = Text("SaveError", card, "", new Vector2(0, -215), new Vector2(740, 150), 26);
            overlay.gameObject.SetActive(false);
        }

        private static RectTransform CanvasRoot(string name)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = go.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 0.5f;
            if (!UnityEngine.Object.FindFirstObjectByType<EventSystem>()) new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            return go.GetComponent<RectTransform>();
        }
        private static RectTransform Design(RectTransform parent)
        {
            RectTransform design = Node("Design1920x1080", parent); Rect(design, Vector2.zero, new Vector2(1920, 1080));
            ResponsiveTitleLayout layout = parent.gameObject.AddComponent<ResponsiveTitleLayout>(); layout.design = design;
            return design;
        }
        private static RectTransform Overlay(string name, RectTransform parent)
        {
            RectTransform root = Node(name, parent); root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one; root.offsetMin = root.offsetMax = Vector2.zero;
            var image = root.gameObject.AddComponent<Image>(); image.color = new Color(0.22f, 0.12f, 0.25f, 0.60f); return root;
        }
        private static RectTransform Panel(string name, RectTransform parent, Vector2 position, Vector2 size, Color color)
        {
            RectTransform rect = Node(name, parent); Rect(rect, position, size);
            var image = rect.gameObject.AddComponent<Image>(); image.sprite = rounded; image.type = Image.Type.Sliced; image.color = color;
            var shadow = rect.gameObject.AddComponent<Shadow>(); shadow.effectColor = new Color(0.4f, 0.18f, 0.33f, 0.17f); shadow.effectDistance = new Vector2(0, -7);
            return rect;
        }
        private static TMP_Text Text(string name, RectTransform parent, string text, Vector2 position, Vector2 size, float fontSize)
        {
            RectTransform rect = Node(name, parent); Rect(rect, position, size);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>(); label.font = font; label.text = text; label.fontSize = fontSize;
            label.alignment = TextAlignmentOptions.Center; label.color = Ink; label.raycastTarget = false; label.textWrappingMode = TextWrappingModes.Normal;
            return label;
        }
        private static Button Button(string label, RectTransform parent, Vector2 position, Vector2 size, Color color)
        {
            RectTransform rect = Panel(label + "Button", parent, position, size, color);
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = rect.GetComponent<Image>();
            ColorBlock colors = button.colors; colors.normalColor = Color.white; colors.highlightedColor = new Color(0.88f, 0.96f, 1f);
            colors.pressedColor = new Color(1f, 0.73f, 0.84f); colors.selectedColor = new Color(0.95f, 0.89f, 1f);
            colors.disabledColor = new Color(0.80f, 0.79f, 0.84f, 0.72f); colors.fadeDuration = 0.12f; button.colors = colors;
            Text("Label", rect, label, Vector2.zero, size - new Vector2(20, 2), 29); return button;
        }
        private static Slider Slider(string label, RectTransform parent, float y, float min, float max, out TMP_Text value)
        {
            Text(label, parent, label, new Vector2(-310, y), new Vector2(340, 60), 28);
            var go = DefaultControls.CreateSlider(new DefaultControls.Resources { standard = rounded, background = rounded });
            go.name = label + "Slider"; go.transform.SetParent(parent, false);
            Rect(go.GetComponent<RectTransform>(), new Vector2(110, y), new Vector2(460, 40));
            var slider = go.GetComponent<Slider>(); slider.minValue = min; slider.maxValue = max;
            slider.fillRect.GetComponent<Image>().color = new Color(0.96f, 0.59f, 0.74f);
            slider.handleRect.GetComponent<Image>().color = Color.white;
            var handleOutline = slider.handleRect.gameObject.AddComponent<Outline>();
            handleOutline.effectColor = new Color(0.80f, 0.40f, 0.58f); handleOutline.effectDistance = new Vector2(2, -2);
            value = Text(label + "Value", parent, "", new Vector2(425, y), new Vector2(120, 55), 27); return slider;
        }
        private static RectTransform Node(string name, RectTransform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)); var rect = go.GetComponent<RectTransform>(); rect.SetParent(parent, false); return rect;
        }
        private static void Rect(RectTransform rect, Vector2 position, Vector2 size)
        { rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f); rect.anchoredPosition = position; rect.sizeDelta = size; }
        private static void Folder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/'); Folder(parent); AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
        private static void CreateRoundedSprite()
        {
            const int size = 64; Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(14f - x, x - 49f, 0f), dy = Mathf.Max(14f - y, y - 49f, 0f);
                float alpha = Mathf.Clamp01(14.5f - Mathf.Sqrt(dx * dx + dy * dy)); texture.SetPixel(x, y, new Color(1, 1, 1, alpha));
            }
            texture.Apply(); string path = Root + "/UI/CandyRounded.png"; File.WriteAllBytes(path, texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path); var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite; importer.spriteBorder = new Vector4(16, 16, 16, 16); importer.mipmapEnabled = false; importer.SaveAndReimport();
            rounded = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
