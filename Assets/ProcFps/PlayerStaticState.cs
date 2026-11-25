using System;
using UnityEngine;

namespace ProcFps
{
    public class PlayerStaticState : MonoBehaviour
    {
        [SerializeField]
        public AimingSettings aiming;
        public ProceduralSwaySettings sway;
        public ProceduralBobSettings bob;
        public ProceduralRecoilSettings recoil;
        
        public Camera playerCamera;
        public Transform leftHandHintTr;
    }

    [Serializable]
    public struct AimingSettings
    {
        public Vector3 hipPosition; // Normal duruş (Göğüs hizası)
        public Vector3 adsPosition; // Nişan alma pozisyonu (Göz hizası)
        public Vector3 leftHandHintHipPosition;
        public Vector3 leftHandHintAdsPosition;
        
        public float adsSpeed;
        
        public float normalFOV;
        public float adsFOV;
    }
    
    [Serializable]
    public class ProceduralSwaySettings
    {
        [Header("Pozisyon Sallanması (Position Sway)")]
        public float swayAmount = 0.02f;      // Sallanma şiddeti
        public float maxSwayAmount = 0.06f;   // Maksimum kayma sınırı
        public float smoothAmount = 6f;       // Toparlanma yumuşaklığı

        [Header("Rotasyon Eğilmesi (Tilt Sway)")]
        public float rotationSwayAmount = 4f; // Sağa-sola dönerken silahın yatma açısı
        public float maxRotationSway = 10f;
        public float rotationSmooth = 12f;

        [Header("Nişan (ADS) Çarpanı")]
        [Range(0f, 1f)]
        public float adsSwayMultiplier = 0.25f; // Nişan alırken sallanmayı %75 azaltır
    }

    [Serializable]
    public class ProceduralBobSettings
    {
        [Header("Yürüme Sekmesi (Weapon Bobbing)")]
        public float walkBobSpeed = 10f;      // Yürüme adım hızı
        public float walkBobAmount = 0.015f;  // Yürüme sekme yüksekliği
        public float sprintBobSpeed = 14f;    // Koşma adım hızı
        public float sprintBobAmount = 0.03f; // Koşma sekme yüksekliği
        
        public float adsBobMultiplier = 0.2f;   // Nişan alırken yürüme/koşma sekmesi çarpanı
    }
    
    [Serializable]
    public class ProceduralRecoilSettings
    {
        public float fireRate = 0.1f;         // Atış hızı (Saniye cinsinden: 0.1 = Saniyede 10 mermi)
        public float weaponRange = 100f;      // Mermi menzili
        
        [Header("Silah Fiziksel Tepmesi (Weapon Kick)")]
        public Vector3 recoilKickPosition = new Vector3(0f, 0.01f, -0.06f); // Geriye (-Z) ve hafif yukarı tepme
        public Vector3 recoilKickRotation = new Vector3(-4f, 1.5f, 2f);     // Namlu şahlanması (-X) ve sağa-sola titreme
        public float weaponSnappiness = 15f;  // Tepmenin ani vuruş hızı
        public float weaponReturnSpeed = 8f;  // Silahın yerine oturma hızı
        
        [Header("Kamera Tepmesi (Camera Recoil)")]
        public float cameraRecoilX = -2f;     // Kameranın yukarı sekme açısı
        public float cameraRecoilY = 0.8f;    // Kameranın sağa/sola rastgele sekme açısı
        public float cameraSnappiness = 10f;
        public float cameraReturnSpeed = 5f;
        
        [Header("Nişan (ADS) Tepme Çarpanı")]
        [Range(0f, 1f)]
        public float adsRecoilMultiplier = 0.5f; // Nişan alırken tepmeyi %50 azaltır
    }
}