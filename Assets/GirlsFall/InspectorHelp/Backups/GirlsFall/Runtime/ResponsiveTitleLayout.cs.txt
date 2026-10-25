using UnityEngine;

namespace GirlsFall
{
    [ExecuteAlways]
    public sealed class ResponsiveTitleLayout : MonoBehaviour
    {
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
