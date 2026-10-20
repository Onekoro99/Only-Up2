using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace OnlyUpPrototype.Editor
{
    public static class PrototypePlaytestRunner
    {
        private const string PendingKey = "OnlyUpPrototype.SmokeTestPending";
        private static double readyAt;

        [InitializeOnLoadMethod]
        private static void Register()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.delayCall += RunRequestedTests;
        }

        private static void RunRequestedTests()
        {
            string request = Path.Combine(Directory.GetParent(Application.dataPath).FullName, ".onlyup-prototype-test-request");
            if (!File.Exists(request)) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.delayCall += RunRequestedTests;
                return;
            }
            if (!AssetDatabase.LoadMainAssetAtPath(PrototypeSceneBuilder.ScenePath)) return;
            File.Delete(request);
            Run();
        }

        [MenuItem("Tools/Only Up/Run Prototype Smoke Test")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != PrototypeSceneBuilder.ScenePath)
                    throw new InvalidOperationException("Open the prototype scene first.");
                readyAt = EditorApplication.timeSinceStartup + 0.5;
                EditorApplication.update -= ExecuteWhenReady;
                EditorApplication.update += ExecuteWhenReady;
                return;
            }
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Save the current scene first.");
            EditorSceneManager.OpenScene(PrototypeSceneBuilder.ScenePath);
            SessionState.SetBool(PendingKey, true);
            EditorApplication.EnterPlaymode();
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(PendingKey, false)) return;
            SessionState.SetBool(PendingKey, false);
            readyAt = EditorApplication.timeSinceStartup + 0.5;
            EditorApplication.update += ExecuteWhenReady;
        }

        private static void ExecuteWhenReady()
        {
            if (EditorApplication.timeSinceStartup < readyAt) return;
            EditorApplication.update -= ExecuteWhenReady;
            StringBuilder report = new StringBuilder();
            int failures = 0;
            Action<bool, string> check = (pass, description) => { report.AppendLine((pass ? "PASS " : "FAIL ") + description); if (!pass) failures++; };
            try
            {
                GameObject player = GameObject.Find("model_chara");
                PrototypePlayerMotor motor = player.GetComponent<PrototypePlayerMotor>();
                FallTracker falls = player.GetComponent<FallTracker>();
                StageRespawn respawn = player.GetComponent<StageRespawn>();
                ThirdPersonOrbitCamera orbit = Camera.main.GetComponent<ThirdPersonOrbitCamera>();
                PrototypeGoal goal = UnityEngine.Object.FindFirstObjectByType<PrototypeGoal>();
                Animator animator = player.GetComponent<Animator>();
                motor.enabled = falls.enabled = respawn.enabled = orbit.enabled = false;
                Transform spawn = GameObject.Find("StageStart").transform;
                Action<Vector2, bool> step = (input, jump) => {
                    motor.Simulate(input, jump, 0.02f);
                    animator.Update(0.02f);
                    falls.Sample(motor.IsGrounded, motor.VerticalSpeed);
                };
                Action reset = () => { motor.Teleport(spawn.position, spawn.rotation); falls.ResetTracking(); for (int i = 0; i < 20; i++) step(Vector2.zero, false); };
                reset();
                check(motor.IsGrounded, "Spawn settles on start platform");
                check(animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Take 001 3"), "Idle state");
                Vector3 before = player.transform.position;
                for (int i = 0; i < 20; i++) step(Vector2.up, false);
                float straight = Vector3.ProjectOnPlane(player.transform.position - before, Vector3.up).magnitude;
                check(straight > 1f, "Forward movement distance=" + straight);
                check(animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Take 001 1"), "Run state");
                reset();
                before = player.transform.position;
                for (int i = 0; i < 20; i++) step(Vector2.one, false);
                float diagonal = Vector3.ProjectOnPlane(player.transform.position - before, Vector3.up).magnitude;
                check(Mathf.Abs(diagonal - straight) < 0.03f, "Diagonal speed normalized distance=" + diagonal);
                reset();
                float takeoff = player.transform.position.y, peak = takeoff;
                bool jumpState = false;
                for (int i = 0; i < 90; i++)
                {
                    step(Vector2.zero, i == 0);
                    peak = Mathf.Max(peak, player.transform.position.y);
                    jumpState |= animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Take 001 0");
                }
                check(peak - takeoff > 0.8f && jumpState, "Jump rises and enters Jump state height=" + (peak - takeoff));
                check(motor.IsGrounded && animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Take 001 3"), "Landing returns to Idle");
                check(falls.LastReport.Distance > 0.8f && !falls.LastReport.EndedByRespawn, "Apex-to-landing fall report");
                reset();
                step(Vector2.up, true);
                for (int i = 0; i < 65; i++) step(Vector2.up, false);
                check(motor.IsGrounded && animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Take 001 1"), "Moving landing returns to Run");
                reset();
                orbit.ApplyLook(new Vector2(30f, 0f));
                orbit.Tick(1f);
                before = player.transform.position;
                for (int i = 0; i < 15; i++) step(Vector2.up, false);
                Vector3 cameraMove = player.transform.position - before;
                check(cameraMove.x > 0.8f && Mathf.Abs(cameraMove.z) < 0.1f, "Camera-relative W at 90 degrees");
                orbit.ApplyLook(new Vector2(0f, 10000f));
                SerializedObject cameraSettings = new SerializedObject(orbit);
                check(Mathf.Abs(cameraSettings.FindProperty("pitch").floatValue + 15f) < 0.01f, "Lower pitch clamp");
                orbit.ApplyLook(new Vector2(0f, -10000f));
                cameraSettings.Update();
                check(Mathf.Abs(cameraSettings.FindProperty("pitch").floatValue - 70f) < 0.01f, "Upper pitch clamp");
                cameraSettings.FindProperty("yaw").floatValue = 0f;
                cameraSettings.FindProperty("pitch").floatValue = 22f;
                cameraSettings.ApplyModifiedPropertiesWithoutUndo();
                reset();
                GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.name = "TemporaryCameraTestWall";
                wall.transform.position = player.transform.position + new Vector3(0f, 1.2f, -2f);
                wall.transform.localScale = new Vector3(4f, 4f, 0.3f);
                Physics.SyncTransforms();
                orbit.Tick(0.02f);
                check(orbit.CurrentDistance < 2.5f, "Camera retracts in front of wall distance=" + orbit.CurrentDistance);
                UnityEngine.Object.DestroyImmediate(wall);
                motor.Teleport(new Vector3(0f, 6f, 0f), Quaternion.identity);
                falls.ResetTracking();
                for (int i = 0; i < 160; i++) step(Vector2.zero, false);
                check(motor.IsGrounded && falls.LastReport.IsSignificant && falls.LastReport.Distance > 5f, "Significant fall distance=" + falls.LastReport.Distance);
                motor.Teleport(new Vector3(40f, -6f, 0f), Quaternion.identity);
                falls.ResetTracking();
                falls.Sample(false, -10f);
                respawn.Respawn();
                check(Vector3.Distance(player.transform.position, spawn.position) < 0.01f && respawn.RespawnCount == 1, "Void respawn returns to stage start");
                check(falls.LastReport.EndedByRespawn, "Void fall marked separately from landing");
                reset();
                bool routeComplete = true;
                for (int index = 1; index <= 13; index++)
                {
                    Transform platform = GameObject.Find(index == 13 ? "GoalPlatform" : "Platform_" + index.ToString("00")).transform;
                    Vector3 landing = platform.position + Vector3.up * (platform.localScale.y * 0.5f);
                    bool jumped = false, airborne = false, landed = false;
                    for (int frame = 0; frame < 240; frame++)
                    {
                        Vector3 difference = Vector3.ProjectOnPlane(landing - player.transform.position, Vector3.up);
                        Vector3 direction = difference.magnitude > 0.08f ? difference.normalized : Vector3.zero;
                        float platformHalfSize = Mathf.Max(platform.localScale.x, platform.localScale.z) * 0.5f;
                        // Wide landings must be approached with an earlier jump, before the capsule touches their front wall.
                        float launchDistance = Mathf.Max(1.75f, platformHalfSize + motor.Controller.radius + 0.35f);
                        bool launch = !jumped && difference.magnitude <= launchDistance && motor.IsGrounded;
                        if (launch) jumped = true;
                        step(new Vector2(direction.x, direction.z), launch);
                        airborne |= !motor.IsGrounded && jumped;
                        if (airborne && motor.IsGrounded)
                        {
                            landed = Mathf.Abs(player.transform.position.y - landing.y - 0.095f) < 0.2f;
                            break;
                        }
                    }
                    check(landed, "Route jump to " + platform.name + " at height=" + landing.y);
                    if (!landed) { routeComplete = false; break; }
                }
                check(routeComplete, "All ascending platforms traversable with actual CharacterController movement");
                motor.Teleport(goal.transform.position, Quaternion.identity);
                goal.CheckPlayer();
                check(goal.IsReached, "Goal detection");
            }
            catch (Exception error) { failures++; report.AppendLine("EXCEPTION " + error); }
            report.AppendLine("Failures: " + failures);
            string path = PrototypeSceneBuilder.Root + "/Documentation/PlaytestReport.txt";
            File.WriteAllText(path, report.ToString());
            if (failures == 0) Debug.Log("ONLYUP_SMOKE_PASS\n" + report);
            else Debug.LogError("ONLYUP_SMOKE_FAIL\n" + report);
            EditorApplication.ExitPlaymode();
        }
    }
}
