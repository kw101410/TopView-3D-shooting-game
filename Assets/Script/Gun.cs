using System.Collections;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.InputSystem;
using TMPro; // ★ UI(텍스트) 제어하려면 이거 필수임

// ★ 필수: 이 스크립트 쓰는 놈은 무조건 AudioSource가 있어야 한다고 강제함
[RequireComponent(typeof(AudioSource))]
public class Gun : MonoBehaviour
{
    [Header("UI & 카메라 반동")]
    [SerializeField] private CrosshairController crosshair;
    [SerializeField] private CameraRecoil camRecoil;
    [SerializeField] private TMP_Text ammoText; // ★ 탄약 표시할 텍스트 (Legacy Text 아님)

    [Header("총 설정")]
    [SerializeField] private float recoilAmount = 30f;
    public float fireDelay = 0.1f;
    [Header("장전 시스템")]
    [SerializeField] private int maxAmmo = 30;
    [SerializeField] private float reloadTime = 1.5f;
    private int currentAmmo;
    private bool isReloading = false;

    [Header("사운드")]
    [SerializeField] private AudioClip sfxFire;   // 발사 소리 파일
    [SerializeField] private AudioClip sfxReload; // 장전 소리 파일
    [SerializeField][Range(0f, 1f)] private float volume = 1f; // 볼륨 조절
    private AudioSource audioSource; // 스피커

    [Header("프리팹 & 위치")]
    [SerializeField] private Bullet bulletPrefab;
    [SerializeField] private Transform firePoint;

    private IObjectPool<Bullet> bulletPool;
    private float lastFireTime;

    [Header("능력치")]
    [SerializeField] private int gunDamage = 10; // ★ 기본 데미지 설정

    private void Awake()
    {
        bulletPool = new ObjectPool<Bullet>(
            createFunc: CreateBullet,
            actionOnGet: OnGetBullet,
            actionOnRelease: OnReleaseBullet,
            actionOnDestroy: OnDestroyBullet,
            maxSize: 300
        );

        // 스피커 컴포넌트 가져오기 (RequireComponent 덕분에 무조건 있음)
        audioSource = GetComponent<AudioSource>();

        // ★ 쿼터뷰 전용 3D 사운드 세팅 추가
        audioSource.spatialBlend = 1f;
        audioSource.dopplerLevel = 0f;
    }
    public void UpgradeDamage()
    {
        gunDamage += 3;
        Debug.Log($"공격력 강화! 현재 데미지: {gunDamage}");
    }

    private void Start()
    {
        currentAmmo = maxAmmo;
        UpdateAmmoUI(); // ★ 시작할 때 "30 / 30" 표시
    }

    private void OnEnable()
    {
        isReloading = false;
        UpdateAmmoUI(); // 껐다 켜도 갱신
    }

    private void Update()
    {
        // 장전 중이면 아무것도 하지 마라 (가장 먼저 체크)
        if (isReloading) return;

        // 수동 장전
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame && currentAmmo < maxAmmo)
        {
            StartCoroutine(Reload());
            return;
        }

        // 발사 체크
        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            if (Time.time >= lastFireTime + fireDelay)
            {
                if (currentAmmo > 0)
                {
                    Fire();
                    lastFireTime = Time.time;
                }
                else
                {
                    // 탄약 0발일 때 클릭하면 바로 장전
                    StartCoroutine(Reload());
                }
            }
        }
    }

    private IEnumerator Reload()
    {
        isReloading = true;

        // ★ 장전 소리 재생
        if (sfxReload != null)
        {
            audioSource.PlayOneShot(sfxReload, volume);
        }

        // 장전 중일 때는 텍스트로 알려주면 좋음 (선택사항)
        if (ammoText != null) ammoText.text = "Reloading...";
        Debug.Log("장전 중... (철컥)");

        yield return new WaitForSeconds(reloadTime);

        currentAmmo = maxAmmo;
        isReloading = false;

        UpdateAmmoUI(); // ★ 장전 끝났으니 다시 숫자 갱신
        Debug.Log($"장전 완료! 남은 탄약: {currentAmmo}");
    }

    private void Fire()
    {
        currentAmmo--;
        UpdateAmmoUI(); // ★ 쏠 때마다 숫자 깎음

        // ★ 발사 소리 재생
        if (sfxFire != null)
        {
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(sfxFire, volume);
        }

        bulletPool.Get();

        if (crosshair != null) crosshair.OnFire(recoilAmount);
        if (camRecoil != null) camRecoil.FireRecoil();

        Bullet bullet = bulletPool.Get();
        bullet.SetDamage(gunDamage);
    }

    // ★ UI 갱신용 헬퍼 함수
    private void UpdateAmmoUI()
    {
        if (ammoText != null)
        {
            ammoText.text = $"{currentAmmo} / {maxAmmo}";
        }
    }

    // --- 풀링 필수 함수들 ---
    private Bullet CreateBullet()
    {
        Bullet bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        bullet.SetPool(bulletPool);
        return bullet;
    }

    private void OnGetBullet(Bullet bullet)
    {
        bullet.transform.position = firePoint.position;

        // ★ 핵심: firePoint의 회전값 중 Y축(좌우)만 가져오고 X, Z(기울기)는 0으로 초기화
        // 이렇게 하면 총구가 하늘을 보고 있어도 총알은 수평으로 나감
        Quaternion flatRotation = Quaternion.Euler(0, firePoint.rotation.eulerAngles.y, 0);
        bullet.transform.rotation = flatRotation;

        bullet.gameObject.SetActive(true);
    }

    private void OnReleaseBullet(Bullet bullet) => bullet.gameObject.SetActive(false);
    private void OnDestroyBullet(Bullet bullet) => Destroy(bullet.gameObject);
}