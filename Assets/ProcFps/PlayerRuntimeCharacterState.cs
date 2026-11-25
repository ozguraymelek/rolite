using System;
using UnityEngine;

namespace ProcFps
{
    public class PlayerRuntimeCharacterState : MonoBehaviour
    {
        public Vector2 moveInput;
        public Vector2 lookInput;
        public bool isSprinting;
        public bool isAiming;
        public bool isFiring;
        
        public bool isAutomatic = true;       // Tam otomatik (basılı tutma) veya tekli atış
        public Transform muzzlePoint;         // Merminin çıkacağı namlu ucu (Muzzle objesi)
        public Transform cameraRecoilHolder;  // Camera_Recoil objesi
        
        public GameObject bulletHolePrefab;
        public float bulletHoleLifetime = 15f;
        public int currentBulletInMag;
        public Vector3 velocity;
    }
}