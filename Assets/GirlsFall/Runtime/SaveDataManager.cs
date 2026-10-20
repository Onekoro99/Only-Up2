using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace GirlsFall
{
    public sealed class SaveDataManager
    {
        private static SaveDataManager instance;
        public static SaveDataManager Instance => instance ?? (instance = new SaveDataManager(Application.persistentDataPath));
        private readonly AtomicSaveFile file;
        private readonly object queueGate = new object(), diskGate = new object();
        private long sequence, committed;
        private string queued;
        private long queuedRevision;
        private Task worker;
        private string lastError;
        public string LastError { get { lock (queueGate) return lastError; } }
        public bool HasSave => file.Exists;
        public string SavePath => file.Path;

        public SaveDataManager(string directory)
        {
            file = new AtomicSaveFile(Path.Combine(directory, "girls-fall-progress.json"));
            if (file.TryRead(out _, out long revision, out _)) sequence = committed = revision;
        }
        public bool TryLoad(out PlayerSaveState state, out string error)
        {
            state = null;
            lock (diskGate)
            {
                if (!file.TryRead(out string payload, out long revision, out error)) return false;
                try
                {
                    state = JsonUtility.FromJson<PlayerSaveState>(payload);
                    if (state == null || !state.IsValid()) { state = null; error = "対応していない、または不正なセーブデータです。"; return false; }
                    sequence = Math.Max(sequence, revision);
                    return true;
                }
                catch (Exception) { error = "セーブデータの読み込みに失敗しました。"; return false; }
            }
        }
        public bool Save(PlayerSaveState state, bool immediate)
        {
            if (state == null || !state.IsValid()) { SetError("保存できないプレイヤー状態です。"); return false; }
            state.savedAtUtc = DateTime.UtcNow.ToString("O");
            long revision = Interlocked.Increment(ref sequence);
            string packed = AtomicSaveFile.Pack(revision, JsonUtility.ToJson(state));
            if (immediate) return Commit(revision, packed);
            lock (queueGate)
            {
                queued = packed; queuedRevision = revision; // Coalesce redundant periodic updates.
                if (worker == null || worker.IsCompleted) worker = Task.Run(Drain);
            }
            return true;
        }
        private void Drain()
        {
            while (true)
            {
                string packed; long revision;
                lock (queueGate)
                {
                    if (queued == null) { worker = null; return; }
                    packed = queued; revision = queuedRevision; queued = null;
                }
                Commit(revision, packed);
            }
        }
        private bool Commit(long revision, string packed)
        {
            lock (diskGate)
            {
                if (revision <= committed) return true; // An older queued write must never overwrite a critical fall snapshot.
                try { file.WritePacked(packed); committed = revision; SetError(null); return true; }
                catch (Exception error) { SetError("保存に失敗しました。空き容量やアクセス権を確認してください。\n" + error.GetType().Name); return false; }
            }
        }
        public void Flush()
        {
            Task active;
            lock (queueGate) active = worker;
            if (active != null) active.GetAwaiter().GetResult();
        }
        private void SetError(string value) { lock (queueGate) lastError = value; }
#if UNITY_EDITOR
        public static void UseStorageForTests(string directory) { if (instance != null) instance.Flush(); instance = new SaveDataManager(directory); }
#endif
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic() { instance = null; }
    }
}
