using UnityEngine;

/// <summary>
/// 대미지 상호작용 기본 인터페이스
/// </summary>
public interface IDamageable
{
    public bool ApplyDamage(float _damage);
}