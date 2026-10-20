using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OnlyUpPrototype.Editor
{
    [InitializeOnLoad]
    public static class PrototypeSceneBuilder
    {
        public const string Root = "Assets/OnlyUpPrototype";
        public const string ScenePath = Root + "/Scenes/OnlyUp_Prototype.unity";
        private static string RequestPath => Path.Combine(Directory.GetParent(Application.dataPath).FullName, ".onlyup-prototype-build-request");

        static PrototypeSceneBuilder() { EditorApplication.delayCall += AutoBuild; }
        private static void AutoBuild()
        {
            if (!File.Exists(RequestPath) || !Application.unityVersion.StartsWith("6000.")) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += AutoBuild;
                return;
            }
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (SceneManager.GetActiveScene().isDirty)
            {
                Debug.LogWarning("Only Up: existing scene has unsaved changes. Save it, then use Tools/Only Up/Create Playable Prototype.");
                return;
            }
            try { Build(); }
            catch (Exception error) { Debug.LogException(error); }
        }

        [MenuItem("Tools/Only Up/Create Playable Prototype")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play mode before building.");
            if (SceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Save your current scene first; it will not be overwritten.");
            if (AssetDatabase.LoadMainAssetAtPath(ScenePath)) throw new InvalidOperationException("Prototype scene already exists; refusing to overwrite it.");
            Folder(Root + "/Scenes");
            Folder(Root + "/Materials");
            Folder(Root + "/Documentation");
            if (!AssetDatabase.CopyAsset("Assets/Scenes/kiyota.unity", ScenePath)) throw new InvalidOperationException("Could not duplicate kiyota.");
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject player = GameObject.Find("model_chara");
            if (!player) throw new InvalidOperationException("model_chara not found.");
            // The legacy script remains intact, and is disabled only in this new scene.
            foreach (MonoBehaviour old in player.GetComponents<MonoBehaviour>())
                if (old && old.GetType().Name == "CharacterMovement") old.enabled = false;
            CharacterController controller = player.GetComponent<CharacterController>();
            if (!controller) controller = player.AddComponent<CharacterController>();
            Animator animator = player.GetComponent<Animator>();
            if (!animator || !animator.runtimeAnimatorController) throw new InvalidOperationException("Existing animator is required.");
            animator.applyRootMotion = false;
            player.transform.SetPositionAndRotation(new Vector3(0f, 0.12f, 0f), Quaternion.identity);
            PrototypePlayerMotor motor = player.AddComponent<PrototypePlayerMotor>();
            FallTracker falls = player.AddComponent<FallTracker>();
            StageRespawn respawn = player.AddComponent<StageRespawn>();
            GameObject stage = new GameObject("PrototypeStage");
            stage.transform.SetAsLastSibling();

            Material startMat = MaterialAt("Start", new Color(0.14f, 0.27f, 0.32f));
            Material platformMat = MaterialAt("Platforms", new Color(0.15f, 0.58f, 0.60f));
            Material accentMat = MaterialAt("Landings", new Color(0.29f, 0.38f, 0.52f));
            Material goldMat = MaterialAt("Goal", new Color(1f, 0.68f, 0.18f));
            GameObject oldGround = GameObject.Find("TestGround");
            if (oldGround) oldGround.SetActive(false);
            Cube("StartPlatform", new Vector3(0f, -0.25f, 0f), new Vector3(8f, 0.5f, 8f), startMat, stage.transform);
            Label("START", new Vector3(0f, 0.03f, 1.6f), stage.transform, 0.4f);
            GameObject spawn = new GameObject("StageStart");
            spawn.transform.SetParent(stage.transform);
            spawn.transform.position = player.transform.position;
            float[] xs = { 0f, 2f, 3f, 1f, -1f, -3f, -1f, 1f, 3f, 1f, 0f, -2f };
            Vector3 last = Vector3.zero;
            for (int i = 0; i < xs.Length; i++)
            {
                float height = 0.6f * (i + 1);
                last = new Vector3(xs[i], height, 4.8f + i * 2.2f);
                float width = i == 4 || i == 8 ? 3.2f : 2.5f;
                Cube("Platform_" + (i + 1).ToString("00"), last - Vector3.up * 0.2f,
                    new Vector3(width, 0.4f, 2.5f), i % 2 == 0 ? platformMat : accentMat, stage.transform);
                Label((i + 1).ToString("00"), last + Vector3.up * 0.04f, stage.transform, 0.32f);
            }
            Vector3 goalTop = last + new Vector3(0f, 0.6f, 2.6f);
            Cube("GoalPlatform", goalTop - Vector3.up * 0.25f, new Vector3(4f, 0.5f, 4f), goldMat, stage.transform);
            GameObject goalPoint = new GameObject("StageGoal");
            goalPoint.transform.SetParent(stage.transform);
            goalPoint.transform.position = goalTop + Vector3.up * 0.2f;
            PrototypeGoal goal = goalPoint.AddComponent<PrototypeGoal>();
            SetObject(goal, "player", player.transform);
            Label("GOAL", goalTop + Vector3.up * 0.04f, stage.transform, 0.5f);
            // A wall near the start makes camera collision easy to verify.
            Cube("CameraCollisionTestWall", new Vector3(-4.8f, 1.5f, 0f), new Vector3(0.4f, 3.5f, 7f), accentMat, stage.transform);

            Camera camera = Camera.main;
            if (!camera) throw new InvalidOperationException("Existing Main Camera is required.");
            camera.nearClipPlane = 0.08f;
            ThirdPersonOrbitCamera orbit = camera.gameObject.AddComponent<ThirdPersonOrbitCamera>();
            SetObject(orbit, "target", player.transform);
            SetObject(motor, "movementCamera", camera.transform);
            SetObject(respawn, "startPoint", spawn.transform);
            SetObject(respawn, "orbitCamera", orbit);
            orbit.SnapToTarget();
            GameObject systems = new GameObject("PrototypeHUD");
            PrototypeHUD hud = systems.AddComponent<PrototypeHUD>();
            SetObject(hud, "player", player.transform);
            SetObject(hud, "falls", falls);
            SetObject(hud, "respawn", respawn);
            SetObject(hud, "goal", goal);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save prototype scene.");
            string report = "Unity " + Application.unityVersion + "\nScene: " + ScenePath +
                "\nOriginal kiyota, controller and clips retained.\n12 ascending platforms + goal; old movement disabled only in prototype.\n";
            File.WriteAllText(Root + "/Documentation/BuildReport.txt", report);
            AssetDatabase.ImportAsset(Root + "/Documentation/BuildReport.txt");
            if (File.Exists(RequestPath)) File.Delete(RequestPath);
            Debug.Log("ONLYUP_BUILD_OK " + ScenePath);
        }

        private static void Folder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            Folder(Path.GetDirectoryName(path).Replace('\\', '/'));
            AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\', '/'), Path.GetFileName(path));
        }
        private static Material MaterialAt(string name, Color color)
        {
            string path = Root + "/Materials/" + name + ".mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing) return existing;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (!shader) shader = Shader.Find("Standard");
            Material material = new Material(shader) { name = name, color = color };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
        private static GameObject Cube(string name, Vector3 position, Vector3 scale, Material material, Transform parent)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent);
            cube.transform.position = position;
            cube.transform.localScale = scale;
            cube.GetComponent<Renderer>().sharedMaterial = material;
            return cube;
        }
        private static void Label(string text, Vector3 position, Transform parent, float size)
        {
            GameObject label = new GameObject("Label_" + text);
            label.transform.SetParent(parent);
            label.transform.SetPositionAndRotation(position, Quaternion.Euler(90f, 0f, 0f));
            TextMesh mesh = label.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.fontSize = 64;
            mesh.characterSize = size * 0.1f;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = Color.white;
        }
        private static void SetObject(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
