using UnityEngine;

namespace OnlyUpPrototype
{
    public sealed class PrototypeHUD : MonoBehaviour
    {
        [SerializeField]
        [Header("デバッグ表示の参照")]
        [Tooltip("高さと最高到達点を表示する対象のTransformです。デバッグ用HUDの参照で、プレイヤーの移動処理は変更しません。")]
        private Transform player;
        [SerializeField]
        [Tooltip("最後の落下距離と現在の落下状態をデバッグ表示するためのFallTrackerです。")]
        private FallTracker falls;
        [SerializeField]
        [Tooltip("スタート地点への復帰回数をデバッグ表示するStageRespawnです。")]
        private StageRespawn respawn;
        [SerializeField]
        [Tooltip("ゴール到達の表示に使用するPrototypeGoalです。この参照を変更してもゴール自体の判定設定は変わりません。")]
        private PrototypeGoal goal;
        private float highest;
        private GUIStyle title, body;
        private void Update() { if (player) highest = Mathf.Max(highest, player.position.y); }
        private void OnGUI()
        {
            if (!player || !falls || !respawn || !goal) return;
            if (title == null)
            {
                title = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };
                title.normal.textColor = new Color(0.4f, 0.95f, 0.9f);
                body = new GUIStyle(GUI.skin.label) { fontSize = 15 };
                body.normal.textColor = Color.white;
            }
            GUI.Box(new Rect(16, 16, 350, 176), GUIContent.none);
            GUI.Label(new Rect(30, 25, 330, 32), "ONLY UP / PROTOTYPE", title);
            GUI.Label(new Rect(30, 62, 330, 25), "WASD Move   |   SPACE Jump", body);
            GUI.Label(new Rect(30, 87, 330, 25), "Hold RMB + Mouse to orbit", body);
            GUI.Label(new Rect(30, 112, 330, 25), $"Height {player.position.y:F1}m   Best {highest:F1}m", body);
            GUI.Label(new Rect(30, 137, 330, 25), $"Last fall {falls.LastReport.Distance:F1}m   Resets {respawn.RespawnCount}", body);
            GUI.Label(new Rect(30, 162, 330, 25), falls.IsFalling ? $"FALLING  {falls.CurrentFallDistance:F1}m" : "Follow the numbered platforms to the gold goal.", body);
            if (goal.IsReached)
            {
                GUI.Box(new Rect(Screen.width / 2f - 180, 32, 360, 70), GUIContent.none);
                GUI.Label(new Rect(Screen.width / 2f - 160, 46, 340, 40), "GOAL REACHED!", title);
            }
        }
    }
}
