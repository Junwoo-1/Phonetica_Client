using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Camera _mainCamera;

    void Start()
    {
        // 씬에서 메인 카메라를 찾아 캐싱합니다. (성능 최적화)
        _mainCamera = Camera.main; 
    }

    // Update가 아닌 LateUpdate를 사용하는 것이 핵심입니다!
    void LateUpdate()
    {
        if (_mainCamera == null) return;

        // 내 회전값을 메인 카메라의 회전값과 완전히 똑같이 맞춥니다.
        // 부모(적)가 아무리 회전해도, 렌더링 직전에 카메라를 정면으로 쳐다보게 강제합니다.
        transform.rotation = _mainCamera.transform.rotation;
    }
}