using UnityEngine;

/// <summary>
/// 8Way Sprite 대신 Eyeball 로 플레이어 방향 피드백
/// </summary>
public class HeadLocator : MonoBehaviour
{
    [SerializeField] private Transform m_Ht; // HeadTransform;

    public void SetAxis(Vector2 _dir)
    {
        float scalar = 0.33f;

        Vector2 fixedDir = new Vector2(
            _dir.x != 0 ? Mathf.Sign(_dir.x) : 0,
            _dir.y != 0 ? Mathf.Sign(_dir.y) : 0
        );

        Vector2 newAxis = fixedDir * scalar;
        m_Ht.localPosition = newAxis;
    }
}
