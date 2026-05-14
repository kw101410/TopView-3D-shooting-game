using UnityEngine;
using TMPro; // UI 써야 하니까
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    // ★ 어디서든 GameManager.Instance 로 부를 수 있게 만드는 마법 (싱글톤)
    public static GameManager Instance;

    [Header("자금 관리")]
    public int money = 0;
    public TMP_Text moneyText; // 화면 구석에 "$0" 표시할 텍스트

    [Header("연결할 것들")]
    public GameObject gameOverPanel;
    public PlayerHealth player;
    public Gun playerGun; // ★ 총 업그레이드 하려면 필요함

    void Awake()
    {
        // 싱글톤 패턴: "나는 오직 하나다"
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        UpdateMoneyUI();

        if (player != null) player.OnDeath += OnPlayerDied;
    }

    // --- 돈 관련 기능 ---
    public void AddMoney(int amount)
    {
        money += amount;
        UpdateMoneyUI();
    }

    public bool SpendMoney(int amount)
    {
        if (money >= amount)
        {
            money -= amount;
            UpdateMoneyUI();
            return true; // 구매 성공
        }
        else
        {
            Debug.Log("돈 없어 임마.");
            return false; // 구매 실패
        }
    }

    void UpdateMoneyUI()
    {
        if (moneyText != null) moneyText.text = $"$ {money}";
    }

    // --- 기존 기능 (게임오버/재시작) ---
    void OnPlayerDied()
    {
        gameOverPanel.SetActive(true);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    void Update()
    {
        // 조건 추가: B키를 눌렀는데 + 휴식 시간(isBreakTime)일 때만!
        if (Input.GetKeyDown(KeyCode.B))
        {
            // WaveManager가 있고 + 휴식 시간이면 OK
            if (WaveManager.Instance != null && WaveManager.Instance.isBreakTime)
            {
                ToggleShop();
            }
            else
            {
                // 전투 중인데 B 누르면? "지금은 못 엽니다" 로그나 띄워줘
                Debug.Log("전투 중에는 상점을 열 수 없습니다!");
            }
        }
    }

    // 상점 열고 닫는 거 함수로 뺌 (깔끔하게)
    void ToggleShop()
    {
        // 이름 틀리지 마라. 하이어라키 패널 이름이랑 똑같아야 함.
        GameObject shop = GameObject.Find("Canvas").transform.Find("ShopPanel").gameObject;

        if (shop != null)
        {
            bool isActive = shop.activeSelf;
            if (isActive) CloseShopLogic(shop); // 켜져 있으면 끄고
            else OpenShopLogic(shop);           // 꺼져 있으면 켜고
        }
    }

    // ★ WaveManager가 호출할 강제 종료 함수
    public void ForceCloseShop()
    {
        GameObject shop = GameObject.Find("Canvas").transform.Find("ShopPanel").gameObject;
        if (shop != null && shop.activeSelf)
        {
            CloseShopLogic(shop);
            Debug.Log("웨이브 시작! 상점 강제 폐쇄.");
        }
    }

    // 내부용: 켜기 로직
    void OpenShopLogic(GameObject shop)
    {
        shop.SetActive(true);
        Time.timeScale = 0f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    // 내부용: 끄기 로직
    void CloseShopLogic(GameObject shop)
    {
        shop.SetActive(false);
        Time.timeScale = 1f;
        Cursor.visible = true;             // 커서 보임
        Cursor.lockState = CursorLockMode.None; // 커서 가두기 해제 (자유롭게 움직임)
    }
}