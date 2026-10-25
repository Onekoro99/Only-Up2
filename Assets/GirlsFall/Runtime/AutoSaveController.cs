using OnlyUpPrototype;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GirlsFall
{
    [DefaultExecutionOrder(40)]
    [RequireComponent(typeof(PrototypePlayerMotor), typeof(FallTracker))]
    public sealed class AutoSaveController : MonoBehaviour
    {
        [SerializeField, Min(0.5f)]
        [Header("自動保存の間隔")]
        [Tooltip("地上でのオートセーブ間隔（秒）です。大きいほど保存回数が減り、強制終了時に戻る可能性のある時間が長くなります。小さいほど頻繁に保存します。初期値3秒です。")]
        private float saveInterval = 3f;
        [SerializeField, Range(0.05f, 1f)]
        [Tooltip("空中・落下中のオートセーブ間隔（秒）です。小さいほど最新の位置・速度を細かく残せますが、保存負荷が増えます。初期値0.15秒です。")]
        private float airborneSaveInterval = 0.15f;
        [SerializeField]
        [Header("進行とゴール")]
        [Tooltip("新規開始時のステージ番号です。ロード時はセーブされた番号に置き換わります。番号だけを変えても別Sceneへは移動しません。初期値1です。")]
        private int stage = 1;
        [SerializeField]
        [Tooltip("突破を保存するゴール判定コンポーネントです。このゴールの到達イベントでステージ完了状態を保存します。")]
        private PrototypeGoal goal;
        private PrototypePlayerMotor motor;
        private FallTracker falls;
        private StageRespawn respawn;
        private double playSeconds;
        private long pv;
        private float elapsed;
        private bool initialized, quitting;
        public bool OpeningDialogueCompleted { get; private set; }
        public bool DialoguePaused { get; set; }
        public bool CanSave { get; private set; }
        public string Error { get; private set; }
        public bool StageCompleted { get; private set; }
        public PlayerSaveState LoadedState { get; private set; }

        private void Awake()
        {
            motor = GetComponent<PrototypePlayerMotor>(); falls = GetComponent<FallTracker>();
            respawn = GetComponent<StageRespawn>();
        }
        private void Start()
        {
            bool fresh = GameSession.NewGameRequested;
            PlayerSaveState state = GameSession.PendingState;
            if (!fresh && state == null && SaveDataManager.Instance.HasSave)
            {
                if (!SaveDataManager.Instance.TryLoad(out state, out string error))
                {
                    Error = error; CanSave = false; initialized = true; GameSession.Ready(); return;
                }
            }
            if (state != null) Restore(state);
            CanSave = initialized = true;
            falls.FallStarted += OnFallStarted;
            falls.FallCompleted += OnLanded;
            if (respawn) respawn.Respawned += OnRespawned;
            if (goal) goal.Reached += OnGoal;
            GameSession.Ready();
            GameSettingsManager.Instance.Apply(false);
            // This is a new slot only after successful scene loading, never on the title screen.
            if (fresh || state == null) SaveNow();
        }
        private void LateUpdate()
        {
            if (!initialized || !CanSave || DialoguePaused || Time.timeScale <= 0f || GameSession.IsTransitioning) return;
            playSeconds += Time.deltaTime;
            elapsed += Time.unscaledDeltaTime;
            float interval = !motor.IsGrounded ? airborneSaveInterval : saveInterval;
            if (elapsed >= interval) { elapsed = 0; SaveDataManager.Instance.Save(Capture(), false); }
            Error = SaveDataManager.Instance.LastError;
        }
        public PlayerSaveState Capture()
        {
            return new PlayerSaveState
            {
                sceneName = gameObject.scene.name, stage = stage, pv = pv, playSeconds = playSeconds,
                position = transform.position, rotation = transform.rotation,
                horizontalVelocity = motor.HorizontalVelocity, verticalVelocity = motor.VerticalSpeed,
                grounded = motor.IsGrounded,
                airborne = !motor.IsGrounded || falls.Airborne,
                falling = falls.IsFalling || (!motor.IsGrounded && motor.VerticalSpeed < 0f),
                departureHeight = falls.DepartureHeight, peakHeight = falls.PeakHeight,
                stageCompleted = StageCompleted, openingDialogueCompleted = OpeningDialogueCompleted
            };
        }
        public void Restore(PlayerSaveState state)
        {
            OpeningDialogueCompleted = state.openingDialogueCompleted;
            LoadedState = JsonUtility.FromJson<PlayerSaveState>(JsonUtility.ToJson(state));
            motor.RestoreRuntimeState(state.position, state.rotation, state.horizontalVelocity, state.verticalVelocity, state.grounded && !state.falling);
            falls.RestoreRuntimeState(state.airborne || state.falling, state.falling, state.departureHeight, state.peakHeight);
            stage = state.stage; pv = state.pv; playSeconds = state.playSeconds; StageCompleted = state.stageCompleted;
            if (goal) goal.RestoreReached(state.stageCompleted);
            var camera = UnityEngine.Object.FindFirstObjectByType<ThirdPersonOrbitCamera>();
            if (camera) camera.SnapToTarget();
        }
        public bool TryCompleteOpeningDialogue()
        {
            bool previous = OpeningDialogueCompleted;
            OpeningDialogueCompleted = true;
            if (SaveNow()) return true;
            OpeningDialogueCompleted = previous;
            return false;
        }
        public bool SaveNow()
        {
            if (!initialized || !CanSave) return false;
            elapsed = 0;
            bool saved = SaveDataManager.Instance.Save(Capture(), true);
            Error = SaveDataManager.Instance.LastError;
            return saved;
        }
        private void OnFallStarted(float height) { SaveNow(); }
        private void OnLanded(FallReport report) { SaveNow(); }
        private void OnRespawned() { SaveNow(); }
        private void OnGoal() { StageCompleted = true; SaveNow(); }
        private void OnApplicationPause(bool paused) { if (paused && !GameSession.IsTransitioning) SaveNow(); }
        private void OnApplicationFocus(bool focused) { if (!focused && !GameSession.IsTransitioning) SaveNow(); }
        private void OnApplicationQuit() { if (!GameSession.IsTransitioning) { SaveNow(); SaveDataManager.Instance.Flush(); } quitting = true; }
        private void OnDestroy()
        {
            if (!quitting && initialized && CanSave && !GameSession.IsTransitioning) SaveNow();
            if (falls) { falls.FallStarted -= OnFallStarted; falls.FallCompleted -= OnLanded; }
            if (respawn) respawn.Respawned -= OnRespawned;
            if (goal) goal.Reached -= OnGoal;
        }
    }
}
