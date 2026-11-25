using UnityEngine;
using UnityEngine.Serialization;

namespace ProcFps
{
    [CreateAssetMenu(fileName = "NewRecoilPattern", menuName = "FPS/Recoil Pattern", order = 0)]
    public class RecoilPattern : ScriptableObject
    {
        [Header("Görsel Sprey Eğrileri (X Ekseni: Mermi Sırası 0 -> 1)")]
        [Tooltip("Dikey (Yukarı) tepme eğrisi. Mermi sırasına göre tepme şiddetini belirler.")]
        public AnimationCurve verticalCurve = new AnimationCurve(
            new Keyframe(0f, 1.8f),   // İlk mermiler sert yükselir
            new Keyframe(0.3f, 1.2f), // Ortaya doğru yükselme azalır
            new Keyframe(1f, 0.3f)    // Son mermilerde artık yukarı çıkmaz, sağa-sola gezer
        );

        [Tooltip("Yatay (Sağa/Sola) tepme eğrisi. + değerler sağa, - değerler sola çeker.")]
        public AnimationCurve horizontalCurve = new AnimationCurve(
            new Keyframe(0f, 0f),     // İlk mermiler düz gider
            new Keyframe(0.25f, 0f),  // 7-8. mermiye kadar düz
            new Keyframe(0.45f, 2.2f),// Sağa kırılır
            new Keyframe(0.75f, -2.5f),// Sola sert savrulur
            new Keyframe(1f, 1.8f)    // Tekrar sağa toparlar
        );

        [FormerlySerializedAs("bulletsSizeInMag")] [Header("Genel Çarpanlar ve Şarjör Uzunluğu")]
        public int bulletsInMag = 30;         // Eğrinin kaç mermiye yayılacağı
        public float verticalMultiplier = 1f; // Dikey tepme genel şiddeti
        public float horizontalMultiplier = 1f; // Yatay tepme genel şiddeti

        [Header("Rastgelelik ve Sıfırlanma")]
        public float randomJitter = 0.2f;     // Mermilere eklenecek doğal sapma payı
        public float patternResetTime = 0.35f;

        // Verilen mermi sırasına (index) göre tepme vektörünü otomatik hesaplar
        public Vector2 GetRecoilStep(int shotIndex)
        {
            // Mermi sırasını 0.0 ile 1.0 arasına oranla (Normalize et)
            float t = Mathf.Clamp01((float)shotIndex / Mathf.Max(1, bulletsInMag - 1));

            float x = horizontalCurve.Evaluate(t) * horizontalMultiplier;
            float y = verticalCurve.Evaluate(t) * verticalMultiplier;

            return new Vector2(x, y);
        }
    }
}