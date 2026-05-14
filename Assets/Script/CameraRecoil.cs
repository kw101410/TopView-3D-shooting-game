using Unity.Cinemachine;
using UnityEngine;

// 1. MonoBehaviour 대신 CinemachineExtension 상속 (필수)
public class CameraRecoil : CinemachineExtension
{
    [Header("반동 설정")]
    [SerializeField] private float recoilX = -2f;
    [SerializeField] private float recoilY = 2f;
    [SerializeField] private float snappiness = 6f;
    [SerializeField] private float returnSpeed = 2f;

    private Vector3 currentRotation;
    private Vector3 targetRotation;

    // 2. Update는 유지하되, 여기서 직접 transform을 건드리지 않음
    // (계산만 해둠)
    void Update()
    {
        targetRotation = Vector3.Lerp(targetRotation, Vector3.zero, returnSpeed * Time.deltaTime);
        currentRotation = Vector3.Slerp(currentRotation, targetRotation, snappiness * Time.deltaTime);
    }

    public void FireRecoil()
    {
        targetRotation += new Vector3(recoilX, Random.Range(-recoilY, recoilY), Random.Range(-0.35f, 0.35f));
    }

    // 3. 시네머신이 카메라 위치/회전 계산을 마친 뒤에 호출되는 함수
    // 여기서 최종 결과물(state)에 반동을 섞어준다.
    protected override void PostPipelineStageCallback(
        CinemachineVirtualCameraBase vcam,
        CinemachineCore.Stage stage,
        ref CameraState state,
        float deltaTime)
    {
        // Aim 단계(카메라가 어디 볼지 결정하는 단계)가 끝났을 때 적용
        if (stage == CinemachineCore.Stage.Aim)
        {
            // 시네머신이 계산한 회전값(RawOrientation)에 내 반동(currentRotation)을 곱함
            Quaternion recoilRot = Quaternion.Euler(currentRotation);
            state.RawOrientation *= recoilRot;
        }
    }
}