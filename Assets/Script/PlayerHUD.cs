using UnityEngine;
using UnityEngine.UI; // UI 건드려야 하니까 필수

public class PlayerHUD : MonoBehaviour
{
    [Header("연결할 것들")]
    public Slider hpSlider;      // 아까 만든 슬라이더
    public PlayerHealth player;  // 니 캐릭터

    void Update()
    {
        // 매 프레임마다 플레이어한테 "너 피 몇 퍼센트 남았냐?" 물어보고 갱신
        if (player != null && hpSlider != null)
        {
            hpSlider.value = player.GetHpRatio();
        }
    }
}