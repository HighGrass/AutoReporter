using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DataSpace;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DataMenu : MonoBehaviour
{
    public int MAX_DATA_FILES { get; private set; } = 6;

    public Dictionary<Data, GameObject> LoadedData { get; private set; } =
        new Dictionary<Data, GameObject>();
    public List<Data> dataOrder = new List<Data>();
    private float defaultHeight;
    private float sizeForData = 10f;

    [SerializeField]
    Button importDataButton;

    [SerializeField]
    RectTransform backgroundRect;

    [SerializeField]
    GameObject dataDisplayPrefab;

    [SerializeField]
    Transform dataDisplayerTransform;

    [SerializeField]
    Sprite openEye;

    [SerializeField]
    Sprite closedEye;

    public Transform GetDisplayerTransform() => dataDisplayerTransform;

    public float MaxDataTime => GetMaxDataTime();

    private float GetMaxDataTime()
    {
        float max_time = 0;
        foreach (Data data in dataOrder)
        {
            foreach ((float time, float value) in data.Values)
            {
                if (time > max_time)
                {
                    max_time = time;
                }
            }
        }
        return max_time;
    }

    public Color[] DataColors { get; private set; } =
        {
            new Color(1, 0.5f, 0.5f, 1),
            new Color(0.5f, 1, 0.5f, 1),
            new Color(0.5f, 0.5f, 1, 1),
            new Color(1f, 0f, 1, 1),
            new Color(1f, 0.5f, 0, 1),
            new Color(0f, 1, 1, 1),
        };
    private Vector2 backgroundInitialPosition;
    private Vector2 importButtonInitialPosition;
    private Grid grid;

    public void Start()
    {
        grid = FindAnyObjectByType<Grid>();
        defaultHeight = backgroundRect.rect.height;
        Debug.Log("DefaultHeight: " + defaultHeight);
        backgroundInitialPosition = backgroundRect.transform.localPosition;
        importButtonInitialPosition = importDataButton.transform.localPosition;
    }

    private void SizeMenu()
    {
        float heightDifference = dataOrder.Count * sizeForData;

        backgroundRect.sizeDelta = new Vector2(
            backgroundRect.sizeDelta.x,
            defaultHeight + heightDifference
        );
        backgroundRect.transform.localPosition = new Vector2(
            backgroundInitialPosition.x,
            backgroundInitialPosition.y - heightDifference / 2
        );
        importDataButton.transform.localPosition = new Vector2(
            importButtonInitialPosition.x,
            importButtonInitialPosition.y - heightDifference
        );

        for (int i = 0; i < dataOrder.Count; i++)
        {
            Data data = dataOrder[i];
            if (LoadedData.TryGetValue(data, out GameObject displayer))
            {
                displayer.transform.localPosition = new Vector2(0, -i * sizeForData);
            }
            else
            {
                Debug.LogWarning($"Data {data.Name} na dataOrder não tem um displayer associado.");
            }
        }
        Button[] buttons = GetComponentsInChildren<Button>();
    }

    public void AddData(Data data)
    {
        dataOrder.Add(data);
        GameObject dataDisplay = CreateDataDisplay(data);
        LoadedData.Add(data, dataDisplay);
        SizeMenu();

        grid.CreateDataGraph(data);
        UpdateDisplayersColors();
    }

    private GameObject CreateDataDisplay(Data data)
    {
        GameObject displayer = Instantiate(dataDisplayPrefab);
        displayer.transform.SetParent(dataDisplayerTransform);

        Button[] buttons = displayer.GetComponentsInChildren<Button>();

        buttons[0].onClick.AddListener(() => RemoveData(data));
        buttons[1].onClick.AddListener(() => ChangeVisualModeCustomGraph(data));

        // Corrige o índice ao basear-se em dataOrder
        int dataIndex = dataOrder.IndexOf(data);
        displayer.transform.localPosition = new Vector2(0, -dataIndex * sizeForData);

        string name = data.Name;
        int length = name.Length;
        if (length > 20)
        {
            name = name.Substring(0, 20) + "...";
        }

        displayer.GetComponentInChildren<TMP_Text>().text = name;

        if (DataColors.Length > 0)
        {
            Debug.Log("color index: " + dataIndex);
            displayer.GetComponentInChildren<Image>().color = DataColors[dataIndex];
        }
        else
        {
            Debug.LogWarning("DataColors array is empty. No color assigned.");
        }

        Debug.Log($"Created DataDisplay for Data: {data.Name}, Index: {dataIndex}");

        return displayer;
    }

    private void RemoveData(Data data)
    {
        if (!LoadedData.ContainsKey(data))
        {
            Debug.LogWarning($"Data {data.Name} não foi encontrado em LoadedData.");
            return;
        }

        // Remova elementos do grid
        GridLine line = LoadedData[data].GetComponentInChildren<GridLine>();
        grid.GetDataGraphs().Remove(line);

        Destroy(line.gameObject);
        // Destrua o GameObject associado
        Destroy(LoadedData[data]);

        // Atualize dataOrder e LoadedData
        LoadedData.Remove(data);
        dataOrder.Remove(data);

        Debug.Log($"Data {data.Name} removido. Itens restantes: {LoadedData.Count}");

        // Atualize o menu e as cores
        UpdateDisplayersColors();
        SizeMenu();
        grid.DestroyGraphKeyframes(data);
    }

    private void UpdateDisplayersColors()
    {
        for (int i = 0; i < dataOrder.Count; i++)
        {
            Data data = dataOrder[i];
            if (!LoadedData.ContainsKey(data))
            {
                Debug.LogWarning(
                    $"Data {data.Name} não encontrado em LoadedData durante a atualização de cores."
                );
                continue;
            }

            GameObject displayer = LoadedData[data];
            GridLine l = displayer.GetComponentInChildren<GridLine>();
            Image image = displayer.GetComponentInChildren<Image>();

            int colorIndex = i % DataColors.Length;
            image.color = DataColors[colorIndex];
            l.Renderer.startColor = DataColors[colorIndex];
            l.Renderer.endColor = DataColors[colorIndex];
        }
    }

    public void ChangeVisualModeCustomGraph(Data data)
    {
        GridLine line = grid.GetDataCustomLine(data);

        if (line.Visible)
        {
            line.Hide();
            HideGraphKeyframes(data);
            SwipeVisualIcon(data, closedEye);
        }
        else
        {
            line.Show();
            ShowGraphKeyframes(data);
            SwipeVisualIcon(data, openEye);
        }
    }

    private void ShowGraphKeyframes(Data data)
    {
        foreach (GameObject keyframe in grid.Keyframes[data])
        {
            keyframe.SetActive(true);
        }
    }

    private void HideGraphKeyframes(Data data)
    {
        foreach (GameObject keyframe in grid.Keyframes[data])
        {
            keyframe.SetActive(false);
        }
    }

    private void SwipeVisualIcon(Data data, Sprite sprite)
    {
        Button[] buttons = LoadedData[data].GetComponentsInChildren<Button>();
        buttons[1].image.sprite = sprite;
    }
}
