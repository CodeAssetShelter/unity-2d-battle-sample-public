using UnityEngine;

/// <summary>
/// Y회전 없이 Visual의 localScale.x 부호만 수정.
/// </summary>
public sealed class Facing2D : MonoBehaviour
{
    [SerializeField] private Transform m_Visual;    // 스프라이트가 붙은 자식
    [SerializeField] private bool m_InvertX = false; // 좌우 반전 필요 시 체크

    /// <summary>월드 기준 XZ 방향 벡터로 좌/우를 판정.</summary>
    public void FaceXZ(Vector3 dirXZ)
    {
        if (!m_Visual) return;
        dirXZ.y = 0f;
        if (dirXZ.sqrMagnitude < 1e-6f) return;

        // 오른(+X) = +1, 왼(-X) = -1. Z는 무시(횡스크롤 우선).
        float sign = Mathf.Sign(dirXZ.x != 0 ? dirXZ.x : Vector3.Dot(dirXZ, Vector3.right));

        var s = m_Visual.localScale;
        s.x = Mathf.Abs(s.x) * (sign >= 0 ? 1f : -1f);
        m_Visual.localScale = s;
    }
}
