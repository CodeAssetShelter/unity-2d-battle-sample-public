using UnityEngine;

public class StoppedParticleSystem : MonoBehaviour
{
    public ParticleSystem m_Target;

    void OnParticleSystemStopped()
    {
        Pool.Despawn(m_Target);
    }
}
