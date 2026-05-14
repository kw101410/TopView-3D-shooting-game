using UnityEngine;
using UnityEngine.InputSystem; // Input System 필수

[RequireComponent(typeof(LineRenderer))]
public class LaserSight : MonoBehaviour
{
    [Header("필수 설정")]
    [SerializeField] private LayerMask groundLayer;   // 마우스 위치 찾기용 (Ground)
    [SerializeField] private LayerMask obstacleLayer; // 벽 막힘 체크용 (Wall, Default 등)

    private LineRenderer lineRenderer;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
    }

    private void Update()
    {
        // 1. 시작점은 총구
        lineRenderer.SetPosition(0, transform.position);

        // 2. 마우스가 가리키는 바닥 위치 찾기
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Ray mouseRay = Camera.main.ScreenPointToRay(mouseScreenPos);

        if (Physics.Raycast(mouseRay, out RaycastHit groundHit, Mathf.Infinity, groundLayer))
        {
            // 마우스가 찍은 바닥 위치 (높이는 총구 높이로 맞춤)
            Vector3 targetPosition = groundHit.point;
            targetPosition.y = transform.position.y;

            // 3. 총구에서 마우스 위치까지 장애물이 있는지 체크
            Vector3 direction = (targetPosition - transform.position).normalized;
            float distance = Vector3.Distance(transform.position, targetPosition);

            if (Physics.Raycast(transform.position, direction, out RaycastHit wallHit, distance, obstacleLayer))
            {
                // 중간에 벽이 있으면 벽까지만 그림
                lineRenderer.SetPosition(1, wallHit.point);
            }
            else
            {
                // 장애물 없으면 마우스 위치까지 그림
                lineRenderer.SetPosition(1, targetPosition);
            }
        }
    }
}