using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ProcFps
{
    public class PlayerProceduralRecoilFire : MonoBehaviour
    {
        [Header("References")] 
        public PlayerController player;
        public WeaponReload reload;
        public PlayerRuntimeCharacterState RuntimeCharacterState;
        public PlayerStaticState StaticState;

        public RecoilPattern recoilPattern;

        private float nextTimeToFire;
        private float lastShotTime;
        private int currentPatternIndex = 0;

        // Hedef ve mevcut pozisyon/rotasyon değişkenleri
        private Vector3 currentWeaponPos;
        private Vector3 targetWeaponPos;
        private Vector3 currentWeaponRot;
        private Vector3 targetWeaponRot;

        private Vector3 currentCamRot;
        private Vector3 targetCamRot;

        private void Start()
        {
            if (player.InputActions == null)
            {
                Debug.LogError("Player Input Actions is null");
                return;
            }

            RuntimeCharacterState.currentBulletInMag = recoilPattern.bulletsInMag;

            player.InputActions.Runtime.Fire.performed += ctx =>
            {
                RuntimeCharacterState.isFiring = true;
                if (!RuntimeCharacterState.isAutomatic) TryShoot(); // Tekli atış moduysa tıklandığı an ateş et
            };
            player.InputActions.Runtime.Fire.canceled += ctx => RuntimeCharacterState.isFiring = false;
        }

        void Update()
        {
            if (!RuntimeCharacterState.isFiring && recoilPattern != null &&
                Time.time - lastShotTime > recoilPattern.patternResetTime)
            {
                currentPatternIndex = 0;
            }
            
            if (RuntimeCharacterState.currentBulletInMag > 0)
            {
                if (RuntimeCharacterState.isAutomatic && RuntimeCharacterState.isFiring && Time.time >= nextTimeToFire)
                {
                    TryShoot();
                }
            }
            
            HandleRecoilReturn();
        }

        void TryShoot()
        {
            if (Time.time < nextTimeToFire) return;

            if (recoilPattern != null && Time.time - lastShotTime > recoilPattern.patternResetTime)
            {
                currentPatternIndex = 0;
            }

            nextTimeToFire = Time.time + StaticState.recoil.fireRate;
            lastShotTime = Time.time;

            ApplyPatternRecoil();
            ShootRaycast();
        }

        void ApplyPatternRecoil()
        {
            float mult = RuntimeCharacterState.isAiming ? StaticState.recoil.adsRecoilMultiplier : 1f;

            targetWeaponPos += StaticState.recoil.recoilKickPosition * mult;
            targetWeaponRot += new Vector3(
                StaticState.recoil.recoilKickRotation.x,
                Random.Range(-StaticState.recoil.recoilKickRotation.y, StaticState.recoil.recoilKickRotation.y),
                Random.Range(-StaticState.recoil.recoilKickRotation.z, StaticState.recoil.recoilKickRotation.z)
            ) * mult;

            if (recoilPattern != null)
            {
                // Eğriden mermi sırasına göre tepme değerini al
                Vector2 step = recoilPattern.GetRecoilStep(currentPatternIndex);

                float jitterX = Random.Range(-recoilPattern.randomJitter, recoilPattern.randomJitter);
                float jitterY = Random.Range(-recoilPattern.randomJitter, recoilPattern.randomJitter);

                float pitch = -(step.y + jitterY) * mult;
                float yaw = (step.x + jitterX) * mult;

                targetCamRot += new Vector3(pitch, yaw, 0f);
                currentPatternIndex++;
            }
        }

        void ShootRaycast()
        {
            reload.ConsumeAmmo();
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                Ray ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
                if (Physics.Raycast(ray, out RaycastHit hit, StaticState.recoil.weaponRange))
                {
                    SpawnBulletHole(hit);
                }
            }
        }

        void SpawnBulletHole(RaycastHit hit)
        {
            if (RuntimeCharacterState.bulletHolePrefab == null) return;

            // Z-Fighting (duvarla iç içe geçip titreme) olmasın diye yüzeyden 0.002 birim öne koyuyoruz
            Vector3 spawnPos = hit.point + (hit.normal * 0.002f);

            // Quad objesinin ön yüzü -Z'ye baktığı için "-hit.normal" yönüne çeviriyoruz
            Quaternion spawnRot = Quaternion.LookRotation(-hit.normal);

            GameObject decal = Instantiate(RuntimeCharacterState.bulletHolePrefab, spawnPos, spawnRot);

            // Hareketli bir objeye çarparsa (örneğin kapı veya düşman) onunla birlikte hareket etsin
            decal.transform.SetParent(hit.transform);

            // Rastgele kendi ekseninde döndür ki tüm mermi izleri aynı görünmesin
            decal.transform.Rotate(Vector3.forward, Random.Range(0f, 360f));

            Destroy(decal, RuntimeCharacterState.bulletHoleLifetime);
        }

        void HandleRecoilReturn()
        {
            float currentCamReturn = RuntimeCharacterState.isFiring
                ? StaticState.recoil.cameraReturnSpeed * 0.2f
                : StaticState.recoil.cameraReturnSpeed;

            targetWeaponPos = Vector3.Lerp(targetWeaponPos, Vector3.zero,
                Time.deltaTime * StaticState.recoil.weaponReturnSpeed);
            targetWeaponRot = Vector3.Lerp(targetWeaponRot, Vector3.zero,
                Time.deltaTime * StaticState.recoil.weaponReturnSpeed);
            targetCamRot = Vector3.Lerp(targetCamRot, Vector3.zero, Time.deltaTime * currentCamReturn);

            currentWeaponPos = Vector3.Lerp(currentWeaponPos, targetWeaponPos,
                Time.deltaTime * StaticState.recoil.weaponSnappiness);
            currentWeaponRot = Vector3.Slerp(currentWeaponRot, targetWeaponRot,
                Time.deltaTime * StaticState.recoil.weaponSnappiness);
            currentCamRot = Vector3.Slerp(currentCamRot, targetCamRot,
                Time.deltaTime * StaticState.recoil.cameraSnappiness);

            transform.localPosition = currentWeaponPos;
            transform.localRotation = Quaternion.Euler(currentWeaponRot);

            if (RuntimeCharacterState.cameraRecoilHolder != null)
            {
                RuntimeCharacterState.cameraRecoilHolder.localRotation = Quaternion.Euler(currentCamRot);
            }
        }
    }
}