using UnityEngine;
using UnityEngine.UI;

public class CrosshairController : MonoBehaviour
{
    [Header("조준점 설정")]
    public RectTransform crosshairRect; // 하이에라키의 Crosshair 이미지
    public Image crosshairImage;        // 색깔 바꿀 때 씀

    [Header("반동 설정")]
    public float recoilScale = 1.5f;    // 발사 시 커지는 크기 (1.5배)
    public float recoverySpeed = 5f;    // 원래 크기로 돌아오는 속도

    private Vector3 defaultScale;

    void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Confined;

        // [핵심] 니가 에디터에서 설정해둔 크기를 '기본 크기'로 인식하게 함
        if (crosshairRect != null)
        {
            defaultScale = crosshairRect.localScale;
        }
    }

    void Update()
    {
        if (crosshairRect != null)
        {
            // 2. 마우스 위치 따라다니기
            crosshairRect.position = Input.mousePosition;

            // 3. 반동 회복 (커졌다가 부드럽게 작아짐)
            crosshairRect.localScale = Vector3.Lerp(crosshairRect.localScale, defaultScale, Time.deltaTime * recoverySpeed);
        }

        // 클릭 시 살짝 작아지는 효과 (선택 사항)
        if (Input.GetMouseButtonDown(0))
        {
            // 클릭 순간엔 반응 안 하고 OnFire에서 처리하거나, 여기서 처리하거나 선택
        }
    }

    // Gun.cs에서 호출하는 함수 (이게 없어서 에러 난 거임)
    [Header("반동 감도 조절 (작을수록 조금 커짐)")]
    public float recoilFactor = 0.01f; // 들어오는 값의 10%만 반영
    public float maxScaleLimit = 1.5f; // 아무리 커져도 2배 넘지 마라

    public void OnFire(float recoilStrength)
    {
        if (crosshairRect != null)
        {
            // 1. 들어오는 값에 0.1 곱해서 확 줄임 (5가 들어오면 0.5가 됨)
            float addedScale = recoilStrength * recoilFactor;

            // 2. (기본 크기 + 반동) 계산하되, maxScaleLimit(2배) 넘지 않게 컷
            float finalScale = Mathf.Clamp(1f + addedScale, 1f, maxScaleLimit);

            crosshairRect.localScale = defaultScale * finalScale;
        }
    }

    // 상점 열 때 호출
    public void SetShopState(bool isShopOpen)
    {
        if (crosshairImage == null) return;

        if (isShopOpen)
        {
            crosshairImage.color = Color.yellow; // 상점 열리면 노란색 (예시)
        }
        else
        {
            crosshairImage.color = Color.white;
        }
    }
}