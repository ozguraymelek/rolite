using UnityEngine;

namespace ProcFps
{
    public class PlayerProceduralAiming : MonoBehaviour
    {
        [Header("References")] 
        public PlayerRuntimeCharacterState RuntimeCharacterState;
        public PlayerStaticState StaticState;
    
        void Start()
        {
            // Başlangıç pozisyonunu otomatik olarak mevcut Hip Position alabilirsin
            transform.localPosition = StaticState.aiming.hipPosition;
            StaticState.leftHandHintTr.localPosition = StaticState.aiming.leftHandHintHipPosition;
        }

        private void Update()
        {
            // Hedef pozisyonu belirle ve yumuşak geçiş (Lerp) yap
            var targetPos = RuntimeCharacterState.isAiming ? StaticState.aiming.adsPosition : StaticState.aiming.hipPosition;
            transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, Time.deltaTime * StaticState.aiming.adsSpeed);
        
            if (StaticState.playerCamera != null)
            {
                var targetFOV = RuntimeCharacterState.isAiming ? StaticState.aiming.adsFOV : StaticState.aiming.normalFOV;
                StaticState.playerCamera.fieldOfView = Mathf.Lerp(StaticState.playerCamera.fieldOfView, targetFOV, Time.deltaTime * StaticState.aiming.adsSpeed);
            }

            var targetHintPos = RuntimeCharacterState.isAiming
                ? StaticState.aiming.leftHandHintAdsPosition
                : StaticState.aiming.leftHandHintHipPosition;
        
            StaticState.leftHandHintTr.localPosition = Vector3.Lerp(StaticState.leftHandHintTr.localPosition, targetHintPos, Time.deltaTime * StaticState.aiming.adsSpeed);

        }
    }
}
