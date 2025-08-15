using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    private static GameManager m_Instance;
    public static GameManager Instance
    {
        get
        {
            if (m_Instance == null)
            {
                m_Instance = FindAnyObjectByType<GameManager>();
                if (m_Instance == null)
                {
                    var obj = new GameObject(nameof(GameManager));
                    m_Instance = obj.AddComponent<GameManager>();
                }
            }
            return m_Instance;
        }
    }

    [Header("Playable Prefabs")]
    [SerializeField] private Transform m_PlayerSpawnArea;
    [SerializeField] private Transform m_EnemySpawnArea;
    [SerializeField] private PlayerMovement m_Player;
    [SerializeField] private BossController m_Enemy;

    private InputAction m_SpaceAction;
    private PlayerMovement _player;
    private BossController _enemy;
    private bool _gameover = false;
    private bool _gamestart = false;

    private void Awake()
    {
        Application.targetFrameRate = 60;
    }
    private IEnumerator Start()
    {
        _gameover = false;
        _gamestart = false;

        m_SpaceAction = new InputAction("Jump", InputActionType.Button);
        m_SpaceAction.AddBinding("<Keyboard>/space");
        m_SpaceAction.AddBinding("<Keyboard>/F");
        m_SpaceAction.AddBinding("<Pointer>/press").WithInteraction("press");

        yield return new WaitForSeconds(1.0f);

        m_SpaceAction.Enable();
        m_SpaceAction.started += OnSpaceStarted;
    }
    private void OnSpaceStarted(InputAction.CallbackContext ctx)
    {
        if (_gameover)
        {
            m_SpaceAction.Disable();
            SceneManager.LoadScene("Sample_2_5D");
        }
        else
            StartGame();
    }

    public void StartGame()
    {
        if (_gamestart) return;

        m_SpaceAction.Disable();
        _gamestart = true;
        UIManager.Instance?.ShowAllButtons(true);
        UIManager.Instance?.m_ReadyText.gameObject.SetActive(false);
        _player = Instantiate(m_Player, m_PlayerSpawnArea);
        _enemy = Instantiate(m_Enemy, m_EnemySpawnArea);

        FindFirstObjectByType<CameraRig>().SetPlayer(_player.transform);

        StartCoroutine(CorStartGame());
    }

    IEnumerator CorStartGame()
    {
        _player.GetComponent<ILockPlayer>().LockPlayer(false);

        yield return new WaitForSeconds(2.5f);

        _enemy.GetComponent<BossController>().LaunchBoss();
    }

    public void NaJugoSSoyo(Transform _pivot)
    {
        UIManager.Instance?.ShowAllButtons(false);

        if (_player != null) _player.GetComponent<ILockPlayer>().LockPlayer(true);
        if (_enemy != null) _enemy.GetComponent<BossController>().StopBoss();

        StartCoroutine(CorNaJugoSSoyo(_pivot.position));
        Destroy(_pivot.gameObject);
    }

    IEnumerator CorNaJugoSSoyo(Vector3 _pivot)
    {
        float elasped = 0;
        float duration = 1.5f;
        var wait = new WaitForSeconds(0.1f);
        while (elasped < duration)
        {
            elasped += 0.1f;
            Vector3 randomDirection = Random.onUnitSphere;
            float randomDistance = Random.Range(0f, 0.5f);

            Pool.Spawn<ParticleSystem>(Resources.Load<GameObject>("HitEfx"),
                _pivot + randomDirection * randomDistance, Quaternion.identity);
            yield return wait;
        }

        UIManager.Instance?.m_GameOverText.gameObject.SetActive(true);
        _gameover = true;

        yield return new WaitForSeconds(1.0f);

        m_SpaceAction.Enable();
    }
}
