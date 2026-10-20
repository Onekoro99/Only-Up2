using UnityEngine;

namespace OnlyUpPrototype
{
    public sealed class PrototypeHUD : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private FallTracker falls;
        [SerializeField] private StageRespawn respawn;
        [SerializeField] private PrototypeGoal goal;
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
