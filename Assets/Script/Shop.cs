using UnityEngine;

public class Shop : MonoBehaviour
{
    // 버튼 눌렀을 때 실행될 함수들

    public void BuyHealth()
    {
        int cost = 500;
        // 1. 돈 있는지 확인하고 지불
        if (GameManager.Instance.SpendMoney(cost))
        {
            // 2. 플레이어 체력 회복 (PlayerHealth에 Heal 함수 필요함. 없으면 아래 참고)
            // GameManager를 통해 플레이어에 접근
            var player = GameManager.Instance.player;
            if (player != null)
            {
                // 임시로 그냥 currentHealth를 100으로 만듦 (PlayerHealth에 public 변수라고 가정)
                // 정석은 player.Heal(30); 같은 함수 만드는 것.
                player.currentHealth = player.maxHealth;
                Debug.Log("체력 풀충전 완료!");
            }
        }
    }

    public void BuyFireRate()
    {
        int cost = 1000;
        if (GameManager.Instance.SpendMoney(cost))
        {
            // Gun 스크립트의 발사 딜레이(fireDelay)를 줄여줌
            var gun = GameManager.Instance.playerGun;
            if (gun != null)
            {
                // 10% 빨라지게 (최소 0.05초 제한)
                gun.fireDelay = Mathf.Max(0.05f, gun.fireDelay * 0.9f);
                Debug.Log($"공속 강화! 현재 딜레이: {gun.fireDelay:F3}");
            }
        }
    }

    public void CloseShop()
    {
        gameObject.SetActive(false); // 상점 끄기
        Time.timeScale = 1f; // 게임 다시 진행 (일시정지 풀기)

        // ▼▼▼ 여기부터 중요 ▼▼▼

        // 1. 커서 숨기기 (니가 만든 십자선 UI만 보이게)
        Cursor.visible = false;

        // 2. 탑다운이니까 마우스 가두기 (Locked 아님! Confined임!)
        Cursor.lockState = CursorLockMode.Confined;
    }
}