using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BossController : MonoBehaviour
{
    [SerializeField] private PlayerSkills m_Skills;
    [SerializeField] private Transform m_Player;
    [SerializeField] private SkillStealSystem m_Steal;
    [SerializeField] private BossPatternData m_PatternData;


    private void OnEnable()
    {
        // 디버깅용 위치
    }

    public void LaunchBoss()
    {
        ActiveAllScript(true);

        int i = 0;
        foreach (var p in m_PatternData.m_Patterns)
        {
            StartCoroutine(RunPattern(p, i));
            ++i;
        }
    }

    public void StopBoss()
    {
        ActiveAllScript(false);
        StopAllCoroutines();
    }

    private void ActiveAllScript(bool _isOn)
    {
        MonoBehaviour[] scripts = GetComponents<MonoBehaviour>();

        foreach (var script in scripts)
        {
            // if (script == this) continue;

            script.enabled = _isOn;
        }
    }

    private IEnumerator RunPattern(BossPatternData.BossPatternClass _data, int _skillIdx)
    {
        float elapsed = 0;
        var waitForInputCooldown = new WaitForSeconds(_data.m_InputCooldown);
        var waitForPatternCooldown = new WaitForSeconds(_data.m_PatternCooldown);

        while (true)
        {
            elapsed = 0;
            while (elapsed < _data.m_MaintainInput)
            {
                m_Skills.TryUse(_skillIdx);
                elapsed += _data.m_InputCooldown;
                yield return waitForInputCooldown;
            }
            yield return waitForPatternCooldown;
        }
    }
}
