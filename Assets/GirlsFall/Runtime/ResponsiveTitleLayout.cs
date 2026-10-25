using UnityEngine;

namespace GirlsFall
{
    [ExecuteAlways]
    public sealed class ResponsiveTitleLayout : MonoBehaviour
    {
        [Header("画面サイズへの対応")]
        [Tooltip("1920×1080を基準にしたUI全体の枠です。画面内に収まる倍率へ一括で拡縮し、縦横比を保ちます。通常は接続済みのResponsiveDesignを維持します。")]
        public RectTransform design;
        private void LateUpdate()
        {
            if (!design) return;
            RectTransform canvas = transform as RectTransform;
            float scale = Mathf.Min(canvas.rect.width / 1920f, canvas.rect.height / 1080f);
            design.localScale = Vector3.one * scale;
        }
    }
}
