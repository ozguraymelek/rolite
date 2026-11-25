using System.Collections;
using ProcFps;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.InputSystem;

public class WeaponReload : MonoBehaviour
{
    public PlayerController player;
    public PlayerRuntimeCharacterState RuntimeCharacterState;
    public PlayerStaticState StaticState;
    
    [Header("Mermi ve Şarjör Ayarları")]
    public int maxAmmo = 30;
    public int currentAmmo;
    public int reserveAmmo = 120;
    public float reloadDuration = 2.1f;

    [Header("Animasyon ve IK Referansları")]
    public Animator handsAnimator;
    public Animator weaponAnimator;
    public TwoBoneIKConstraint leftHandIK;
    public float ikBlendSpeed = 10f;

    [Header("Animator Katman İndeksleri")]
    [Tooltip("Animator penceresindeki sıraya göre indeksler (Base Layer = 0'dır)")]
    public int reloadLayerIndex = 1;
    public int leftFingerLayerIndex = 3;

    [HideInInspector]
    public bool isReloading = false;

    private float targetLeftIKWeight = 1f;
    
    public RecoilPattern recoilPattern;
    private int reloadLayerIdx = -1;
    private int leftFingerLayerIdx = -1;
    
    [Header("Animator Katman ve State İsimleri")]
    public string reloadLayerName = "ReloadLayer";
    public string leftFingerLayerName = "LeftFinger_GripLayer";
    public string reloadStateName = "RELOAD1"; // Animator'daki Reload animasyon kutusunun tam adı
    protected static int RELOAD_EMPTY = Animator.StringToHash("Reload_Empty");
    
    void Start()
    {
        currentAmmo = recoilPattern.bulletsInMag;
        player.InputActions.Runtime.Reload.performed += ctx => TryReload();
        
        // Katman indekslerini isimlerinden otomatik bul
        /*if (handsAnimator != null)
        {
            reloadLayerIdx = handsAnimator.GetLayerIndex(reloadLayerName);
            leftFingerLayerIdx = handsAnimator.GetLayerIndex(leftFingerLayerName);

            if (reloadLayerIdx == -1)
                Debug.LogError($"Animator içinde '{reloadLayerName}' adında bir katman bulunamadı!");
        }*/
    }

    void Update()
    {
        /* Sol el IK ağırlığını hedef değere (0 veya 1) yumuşakça geçir
        if (leftHandIK != null)
        {
            //leftHandIK.weight = Mathf.Lerp(leftHandIK.weight, targetLeftIKWeight, Time.deltaTime * ikBlendSpeed);
        }*/

        // IK ağırlığına tam senkronize şekilde Animator katmanlarını yönet
        /*if (handsAnimator != null && leftHandIK != null)
        {
            // IK 1 iken Reload katmanı 0; IK 0 iken Reload katmanı 1 olur
            var reloadWeight = 1f ;
            // IK = 1 iken: Sol parmak tutuşu aktif (1), Reload katmanı kapalı (0)
            // IK = 0 iken: Sol parmak tutuşu kapalı (0), Reload katmanı aktif (1)
            if (reloadLayerIdx != -1)
                handsAnimator.SetLayerWeight(reloadLayerIdx, reloadWeight);
            if (leftFingerLayerIdx != -1)
                handsAnimator.SetLayerWeight(leftFingerLayerIdx, 0);
        }*/
    }

    public void TryReload()
    {
        if (isReloading /*|| currentAmmo == maxAmmo || reserveAmmo <= 0*/) return;

        StartCoroutine(ReloadRoutine());
    }

    IEnumerator ReloadRoutine()
    {
        isReloading = true;

        // 1. Sol el IK'sını kapat (Update otomatik olarak Reload katmanını 1, Sol Parmak katmanını 0 yapar)
        //targetLeftIKWeight = 0f;

        if (handsAnimator != null && weaponAnimator != null)
        {
            handsAnimator.SetTrigger("Reload");
            weaponAnimator.SetTrigger("Reload");
        }
        // 2. Reload animasyonunu en baştan (0. kareden) başlat
        /*if (handsAnimator != null && reloadLayerIdx != -1)
        {
            handsAnimator.Play(reloadStateName, reloadLayerIdx, 0f);
        }*/

        // 3. Animasyonun bitmesine 0.25 sn kalana kadar bekle
        float waitTime = Mathf.Max(0.1f, reloadDuration - 0.25f);
        yield return new WaitForSeconds(waitTime);

        // 4. Sol el IK'sını tekrar aç (El kundağa, parmaklar tutuş pozuna geri döner)
        //targetLeftIKWeight = 1f;

        //yield return new WaitForSeconds(0.25f);

        // 5. Mermi sayılarını hesapla ve güncelle
        int ammoNeeded = maxAmmo - currentAmmo;
        int ammoToLoad = Mathf.Min(ammoNeeded, reserveAmmo);

        currentAmmo += ammoToLoad;
        reserveAmmo -= ammoToLoad;

        isReloading = false;
    }

    public bool ConsumeAmmo()
    {
        if (isReloading || currentAmmo <= 0) return false;

        currentAmmo--;
        return true;
    }
}
