using UnityEngine;

public abstract class DamageSetter : MonoBehaviour
{
    public float m_Damage;
    public abstract Transform _owner { get; set; }

    public abstract void Attack(float _damage, Transform _target);
    // 나중에 여기 추가 스탯 필요하면 작성
}
