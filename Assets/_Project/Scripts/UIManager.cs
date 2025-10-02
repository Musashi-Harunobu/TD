using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Buttons")]
    public Button btnPlaceMode;
    public Button btnDeleteMode;
    public Button btnSave;
    public Button btnExport;
    public Button btnImport;

    [Header("Building buttons")]
    public Button btnHouse;
    public Button btnFactory;
    public Button btnFarm;

    [Header("Labels")]
    public TextMeshProUGUI modeText;

    public void Init()
    {
        btnPlaceMode.onClick.AddListener(() => Game.I.buildController.SetMode(BuildMode.Place));
        btnDeleteMode.onClick.AddListener(() => Game.I.buildController.SetMode(BuildMode.Delete));

        btnSave.onClick.AddListener(() => Game.I.buildController.Save());
        btnExport.onClick.AddListener(() => Game.I.buildController.ExportReadable());
#if UNITY_EDITOR
        btnImport.onClick.AddListener(() =>
        {
            var path = UnityEditor.EditorUtility.OpenFilePanel("Import", Application.persistentDataPath, "json");
            if (!string.IsNullOrEmpty(path)) Game.I.buildController.ImportFrom(path);
        });
#else
        btnImport.onClick.AddListener(() =>
        {
            var path = System.IO.Path.Combine(Application.persistentDataPath, "layout_import.json");
            Game.I.buildController.ImportFrom(path);
        });
#endif

        var db = Game.I.database;
        if (db.All.Count > 0)
        {
            var idHouse   = db.All.FirstOrDefault(b => b.id == "house")?.id   ?? db.All[0].id;
            var idFactory = db.All.FirstOrDefault(b => b.id == "factory")?.id ?? db.All[0].id;
            var idFarm    = db.All.FirstOrDefault(b => b.id == "farm")?.id    ?? db.All[0].id;

            if (btnHouse)   btnHouse.onClick.AddListener(() => Game.I.buildController.SetActiveBuilding(idHouse));
            if (btnFactory) btnFactory.onClick.AddListener(() => Game.I.buildController.SetActiveBuilding(idFactory));
            if (btnFarm)    btnFarm.onClick.AddListener(() => Game.I.buildController.SetActiveBuilding(idFarm));
        }

        UpdateModeText(Game.I.buildController.Mode);
    }

    public void UpdateModeText(BuildMode mode)
    {
        if (!modeText) return;
        modeText.text = mode == BuildMode.Place ? "Режим: Размещение" : "Режим: Удаление";
        modeText.color = mode == BuildMode.Place ? new Color(0.2f,0.9f,0.3f) : new Color(0.95f,0.3f,0.3f);
    }
}
