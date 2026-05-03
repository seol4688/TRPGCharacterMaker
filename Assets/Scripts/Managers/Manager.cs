using TMPro;
using UnityEngine;
using static Define;

public class Manager : MonoBehaviour
{
    static Manager Instance;

        DataManager _data = new DataManager();
    public static DataManager Data
    {
        get
        {
            if (Instance == null)
            {
                Debug.LogError("Manager instance is null. Make sure Manager is present in the scene.");
                return null;
            }

            return Instance._data;
        }
    }

    [SerializeField] private SaveModalController _saveModal;
    public static SaveModalController SaveModal
    {
        get
        {
            if (Instance == null)
            {
                Debug.LogError("Manager instance is null. Make sure Manager is present in the scene.");
                return null;
            }

            return Instance._saveModal;
        }
    }

    [SerializeField] private LoadModalController _loadModal;
    public static LoadModalController LoadModal
    {
        get
        {
            if (Instance == null)
            {
                Debug.LogError("Manager instance is null. Make sure Manager is present in the scene.");
                return null;
            }

            return Instance._loadModal;
        }
    }

    public Rule _rule;

    [Header("Version")]
    [SerializeField] private TMP_Text versionText;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        versionText.text = $"Version {Application.version}";
        Data.Init();
    }

    public void OnClickExit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
