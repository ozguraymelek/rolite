using UnityEngine;

namespace ProcFps
{
    public class WeaponProceduralBobAndSway : MonoBehaviour
    {
        [Header("References")] 
        public PlayerRuntimeCharacterState RuntimeCharacterState;
        public PlayerStaticState StaticState;
        
        private Vector3 _initialPosition;
        private Quaternion _initialRotation;
        private float _bobTimer;
        
        void Start()
        {
            _initialPosition = transform.localPosition;
            _initialRotation = transform.localRotation;
        }

        void Update()
        {
            // Nişan alıyorsak (ADS) sallanma miktarını düşür ki hedef şaşmasın
            var swayMult = RuntimeCharacterState.isAiming ? StaticState.sway.adsSwayMultiplier : 1f;
            var bobMult = RuntimeCharacterState.isAiming ? StaticState.bob.adsBobMultiplier : 1f;
            
            // 1. FARE SALLANMASI (SWAY)
            var moveX = Mathf.Clamp(-RuntimeCharacterState.lookInput.x * StaticState.sway.swayAmount * swayMult, -StaticState.sway.maxSwayAmount, StaticState.sway.maxSwayAmount);
            var moveY = Mathf.Clamp(-RuntimeCharacterState.lookInput.y * StaticState.sway.swayAmount * swayMult, -StaticState.sway.maxSwayAmount, StaticState.sway.maxSwayAmount);
            var swayPosition = new Vector3(moveX, moveY, 0f);
            
            // 2. YÜRÜME SEKMESİ (BOBBING)
            var bobPosition = Vector3.zero;
            if (RuntimeCharacterState.moveInput.magnitude > 0.1f) // Karakter hareket ediyorsa
            {
                // Nişan alırken (ADS) koşma hızı yerine yürüme hızını baz al ki dürbün titremesin
                var effectiveSprint = RuntimeCharacterState.isSprinting && !RuntimeCharacterState.isAiming;
                
                var currentBobSpeed = (effectiveSprint ? StaticState.bob.sprintBobSpeed : StaticState.bob.walkBobSpeed);
                var currentBobAmount = (effectiveSprint ? StaticState.bob.sprintBobAmount : StaticState.bob.walkBobAmount) * bobMult;

                _bobTimer += Time.deltaTime * currentBobSpeed;
            
                // Yatayda (X) tek frekans, dikeyde (Y) çift frekans kullanarak doğal "U" şekli çizdiririz
                bobPosition.x = Mathf.Cos(_bobTimer) * currentBobAmount;
                // "- 0.5f" ekleyerek dikey salınımın merkezini tam 0 noktasına (gez-arpacık hizasına) sabitliyoruz
                bobPosition.y = Mathf.Abs(Mathf.Sin(_bobTimer) - 0.5f) * currentBobAmount;
            }
            else
            {
                // Durduğunda sayacı sıfırla ki silah merkeze yumuşakça otursun
                _bobTimer = 0f;
            }

            // Sway ve Bobbing pozisyonlarını birleştirip uygula
            var targetPosition = _initialPosition + swayPosition + bobPosition;
            transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, Time.deltaTime * StaticState.sway.smoothAmount);

            // 2. Rotasyon Eğilmesi Hesaplama (Silahın sağa-sola yatması)
            var tiltY = Mathf.Clamp(RuntimeCharacterState.lookInput.x * StaticState.sway.rotationSwayAmount * swayMult, -StaticState.sway.maxRotationSway, StaticState.sway.maxRotationSway);
            var tiltX = Mathf.Clamp(RuntimeCharacterState.lookInput.y * StaticState.sway.rotationSwayAmount * swayMult, -StaticState.sway.maxRotationSway, StaticState.sway.maxRotationSway);

            // Z ekseninde hafif yatış (tiltY) silaha gerçekçi bir ağırlık hissi verir
            var finalRotation = Quaternion.Euler(new Vector3(-tiltX, tiltY, -tiltY));
            transform.localRotation = Quaternion.Slerp(transform.localRotation, _initialRotation * finalRotation, Time.deltaTime * StaticState.sway.rotationSmooth);
        }
    }
}
