using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace GirlsFall
{
    // One logical slot. A complete in-flight transaction is also a recovery candidate, never a user-selectable save.
    public sealed class AtomicSaveFile
    {
        [Serializable] private sealed class Envelope
        {
            public int format = 1;
            public long revision;
            public string payload;
            public string checksum;
        }
        public string Path { get; }
        public string PendingPath => Path + ".pending";
        public bool Exists => File.Exists(Path) || File.Exists(PendingPath);
        public AtomicSaveFile(string path) { Path = path; }

        public bool TryRead(out string payload, out long revision, out string error)
        {
            payload = null; revision = 0; error = null;
            bool any = false;
            foreach (string candidate in new[] { Path, PendingPath })
            {
                if (!File.Exists(candidate)) continue;
                any = true;
                try
                {
                    FileInfo info = new FileInfo(candidate);
                    if (info.Length > 1024 * 1024) continue;
                    Envelope envelope = JsonUtility.FromJson<Envelope>(File.ReadAllText(candidate, Encoding.UTF8));
                    if (envelope == null || envelope.format != 1 || envelope.revision <= 0 || string.IsNullOrEmpty(envelope.payload)) continue;
                    if (Hash(envelope.revision, envelope.payload) != envelope.checksum) continue;
                    if (envelope.revision > revision) { revision = envelope.revision; payload = envelope.payload; }
                }
                catch (Exception) { /* A torn transaction must not crash the game. */ }
            }
            if (payload != null) return true;
            error = any ? "セーブデータを読み込めません。データが破損している可能性があります。" : "セーブデータがありません。";
            return false;
        }

        // Prepare JSON on Unity's main thread; workers perform only .NET file IO.
        public static string Pack(long revision, string payload)
        {
            return JsonUtility.ToJson(new Envelope { revision = revision, payload = payload, checksum = Hash(revision, payload) });
        }
        public void WritePacked(string packed)
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path));
            byte[] bytes = new UTF8Encoding(false).GetBytes(packed);
            using (FileStream stream = new FileStream(PendingPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(true);
            }
            if (File.Exists(Path)) File.Replace(PendingPath, Path, null);
            else File.Move(PendingPath, Path);
        }
        private static string Hash(long revision, string payload)
        {
            using (SHA256 sha = SHA256.Create())
                return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(revision.ToString(System.Globalization.CultureInfo.InvariantCulture) + "\n" + payload)));
        }
    }
}
