using UnityEngine;

public class Fire3Chain : MonoBehaviour, ISkill
{
    [SerializeField] private string m_SkillName = "3ComboChain";
    [SerializeField] private float m_Distance = 0.25f;
    [SerializeField] private BulletFireWall3Chain m_AttackPrefab;

    public string SkillName => m_SkillName;

    public bool Execute(Transform owner)
    {
        if (!owner) return false;

        var dp = owner.GetComponent<IInputDirectionProvider>().GetInputDirection();
        var dir = (dp.dir.sqrMagnitude > 0.0001f) ? dp.dir.normalized : owner.forward;

        // Owner를 밀어주던 것도 동일 방향으로
        owner.GetComponent<Rigidbody>().AddForce(dir * m_Distance, ForceMode.Impulse);

        // 위치: owner 기준 dir로 m_Distance 오프셋
        var spawnPos = owner.position + dir;

        // 회전: dir을 바라보게
        var spawnRot = Quaternion.LookRotation(dir, Vector3.up);

        var bolt = Pool.Spawn<BulletFireWall3Chain>(m_AttackPrefab.gameObject, spawnPos, spawnRot, owner);
        bolt.Fire(dp.dir, owner);

        Debug.Log($"[ISkill] {m_SkillName} by {owner.name}");
        return true;
    }
}
