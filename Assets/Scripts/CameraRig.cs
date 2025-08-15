using System.Runtime.InteropServices;
using UnityEngine;

public class CameraRig : MonoBehaviour
{
    [SerializeField] private Transform m_Player;
    [SerializeField] private Transform m_Cam;

    public void SetPlayer(Transform _target) => m_Player = _target;
    private void LateUpdate()
    {
        if (m_Cam == null || m_Player == null) return;
        Vector3 newPos = m_Cam.transform.position;
        newPos.x = m_Player.transform.position.x;
        m_Cam.position = newPos;
    }
}
