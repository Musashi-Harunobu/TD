using UnityEngine;

public class Game : MonoBehaviour
{
    public static Game I { get; private set; }

    [Header("Scene refs")]
    public Camera mainCamera;
    public Transform worldRoot;
    public GameObject buildingPrefab;
    public GameObject ghostPrefab;

    [Header("Services (components on same GO)")]
    public BuildingDatabase database;
    public GridManager gridManager;
    public BuildController buildController;
    public UIManager ui;

    private void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        database.LoadConfigs();
        gridManager.Init();
        buildController.Init();
        ui.Init();
        buildController.LoadOnStart();
    }
}