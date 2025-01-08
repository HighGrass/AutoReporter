using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DataSpace;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum LineOrientation
{
    Horizontal,
    Vertical,
    Custom,
}

[RequireComponent(typeof(Canvas))]
public class Grid : MonoBehaviour
{
    [SerializeField]
    GameObject LinePrefab;

    [SerializeField]
    GameObject KeyframePrefab;

    [SerializeField]
    GameObject CustomLinePrefab;

    [SerializeField]
    Scrollbar ScrollBar;
    public Vector2 Size { get; private set; }
    public float Spacing { get; private set; } = 10;
    private float MIN_SPACING = 3f;
    private float MAX_SPACING = 20f;
    public Vector2 Density => CalculateGridDensity(Size);
    private List<GridLine> HorizontalLines { get; set; }
    private List<GridLine> VerticalLines { get; set; }
    private List<GridLine> InactiveLines { get; set; }
    private List<GridLine> DataGraphs { get; set; } = new List<GridLine>();

    public List<GridLine> GetDataGraphs() => DataGraphs;

    public Dictionary<Data, GameObject[]> Keyframes { get; private set; } =
        new Dictionary<Data, GameObject[]>();

    // --------------------------------------------------------------------

    private Canvas canvas;
    private DataMenu dataMenu;
    private Transform linePlacer;
    private Transform KeyframePlacer;

    [SerializeField]
    Button formatButton;

    [SerializeField]
    Sprite normalizedActive;

    [SerializeField]
    Sprite normalizedInactive;

    [SerializeField]
    TMP_Text timeText;

    public enum FormatMode
    {
        Default,
        Normalized,
    }

    public FormatMode Format { get; private set; } = FormatMode.Default;

    public void ChangeFormat()
    {
        if (Format == FormatMode.Default)
        {
            Format = FormatMode.Normalized;
            SwipeFormatIcon(normalizedActive);
        }
        else
        {
            Format = FormatMode.Default;
            SwipeFormatIcon(normalizedInactive);
        }
    }

    private void SwipeFormatIcon(Sprite sprite)
    {
        formatButton.image.sprite = sprite;
    }

    //private bool ChangeNormalizeTime() => NormalizeTime = !NormalizeTime;

    // --------------------------------------------------------------------

    void Start()
    {
        canvas = GetComponent<Canvas>();

        dataMenu = FindAnyObjectByType<DataMenu>();
        RectTransform rect = canvas.gameObject.GetComponent<RectTransform>();

        linePlacer = transform.GetChild(0);
        KeyframePlacer = transform.GetChild(1);

        Size = new Vector2(rect.rect.width, rect.rect.height);
        CreateGrid();
        ManageGridStructure(); // manage lines
        UpdateGridPositions();

        formatButton.onClick.AddListener(() => ChangeFormat());
    }

    public void Update()
    {
        UpdateSpacing(); // manage mouse scroll
        ManageGridStructure(); // manage lines
        UpdateGridPositions(); // manage lines positions
    }

    void ManageGridStructure()
    {
        if (Density.y < HorizontalLines.Count)
        {
            DeactivateLines(HorizontalLines.Count - (int)Density.y, LineOrientation.Horizontal);
        }
        else if (Density.y > HorizontalLines.Count)
        {
            int amount = (int)Density.y - HorizontalLines.Count;
            ActivateLines(amount, LineOrientation.Horizontal);
        }

        if (Density.x < VerticalLines.Count)
            DeactivateLines(VerticalLines.Count - (int)Density.x, LineOrientation.Vertical);
        else if (Density.x > VerticalLines.Count)
        {
            int amount = (int)Density.x - VerticalLines.Count;
            ActivateLines(amount, LineOrientation.Vertical);
        }
        //ActivateLines((int)Density.y - HorizontalLines.Count, LineOrientation.Horizontal);
        //ActivateLines((int)Density.x - VerticalLines.Count, LineOrientation.Vertical);
    }

    void CreateGrid()
    {
        HorizontalLines = new List<GridLine>();
        VerticalLines = new List<GridLine>();
        InactiveLines = new List<GridLine>();
        for (int i = 0; i < 100; i++)
        {
            GameObject line = new GameObject("line");
            GridLine gl = line.AddComponent<GridLine>();
            gl.Create(this, i, LineOrientation.Horizontal, LinePrefab);
            gl.Renderer.sortingOrder = 10;
            InactiveLines.Add(gl);
            line.transform.SetParent(linePlacer);
        }

        ManageGridStructure();

        GetInitialLine(LineOrientation.Horizontal).Renderer.sortingOrder = 11;
        GetInitialLine(LineOrientation.Vertical).Renderer.sortingOrder = 11;
    }

    Vector2 CalculateGridDensity(Vector2 gridSize)
    {
        Vector2 density = gridSize / Spacing;
        density = new Vector2(Mathf.RoundToInt(density.x), Mathf.RoundToInt(density.y));

        return density;
    }

    private void UpdateGridPositions()
    {
        for (int i = 0; i < HorizontalLines.Count; i++)
        {
            (Vector2 initial, Vector2 final) positions = CalculateLinePosition(
                i,
                LineOrientation.Horizontal,
                Spacing
            );
            HorizontalLines[i]
                .Renderer.SetPositions(new Vector3[] { positions.initial, positions.final });
        }
        for (int i = 0; i < VerticalLines.Count; i++)
        {
            (Vector2 initial, Vector2 final) positions = CalculateLinePosition(
                i,
                LineOrientation.Vertical,
                Spacing
            );
            VerticalLines[i]
                .Renderer.SetPositions(new Vector3[] { positions.initial, positions.final });
        }

        foreach (Data data in dataMenu.LoadedData.Keys)
        {
            UpdateDataGraph(data);
        }
    }

    private void UpdateSpacing()
    {
        float mouseScrollDelta = Input.GetAxis("Mouse ScrollWheel");

        if (Mathf.Abs(mouseScrollDelta) > 0)
        {
            Spacing = Mathf.Clamp(
                Spacing + mouseScrollDelta * Time.deltaTime * 1000,
                MIN_SPACING,
                MAX_SPACING
            );
            ScrollBar.value = GetSpacingPercentage01(Spacing);
        }
        foreach (Data data in dataMenu.dataOrder)
        {
            UpdateGraphKeyframes(data);
        }

        float offset = 5f;
        Vector3 timeTextPos = Vector3.zero;
        timeTextPos.x = GetInitialLine(LineOrientation.Vertical).Renderer.GetPosition(0).x + offset;
        timeTextPos.y =
            GetInitialLine(LineOrientation.Horizontal).Renderer.GetPosition(0).y - offset;
        UpdateTextPosition(timeText, timeTextPos);
    }

    float GetSpacingPercentage01(float spacing)
    {
        float value = (spacing - MIN_SPACING) / (MAX_SPACING - MIN_SPACING);
        return value;
    }

    private void ActivateLines(int ammount, LineOrientation orientation)
    {
        int ammoutLeft = ammount;
        while (ammoutLeft > 0)
        {
            if (InactiveLines.Count <= 0)
                return;

            switch (orientation)
            {
                case LineOrientation.Horizontal:
                {
                    InactiveLines[InactiveLines.Count - 1]
                        .SetOrientation(LineOrientation.Horizontal);
                    InactiveLines[InactiveLines.Count - 1].SetIndex((int)Density.y - ammoutLeft);
                    HorizontalLines.Add(InactiveLines[InactiveLines.Count - 1]);
                    HorizontalLines[HorizontalLines.Count - 1].Show();
                    InactiveLines.RemoveAt(InactiveLines.Count - 1);
                    ammoutLeft--;
                    break;
                }

                case LineOrientation.Vertical:
                {
                    InactiveLines[InactiveLines.Count - 1].SetOrientation(LineOrientation.Vertical);
                    VerticalLines.Add(InactiveLines[InactiveLines.Count - 1]);
                    VerticalLines[VerticalLines.Count - 1].Show();
                    InactiveLines.RemoveAt(InactiveLines.Count - 1);
                    ammoutLeft--;
                    break;
                }
            }
        }
    }

    private void DeactivateLines(int ammount, LineOrientation orientation)
    {
        int ammoutLeft = ammount;
        while (ammoutLeft > 0)
        {
            switch (orientation)
            {
                case LineOrientation.Horizontal:
                {
                    if (HorizontalLines.Count <= 0)
                        return;

                    InactiveLines.Add(HorizontalLines[HorizontalLines.Count - 1]);
                    InactiveLines[InactiveLines.Count - 1].Hide();
                    HorizontalLines.RemoveAt(HorizontalLines.Count - 1);
                    ammoutLeft--;
                    break;
                }

                case LineOrientation.Vertical:
                {
                    if (VerticalLines.Count <= 0)
                        return;

                    InactiveLines.Add(VerticalLines[VerticalLines.Count - 1]);
                    InactiveLines[InactiveLines.Count - 1].Hide();
                    VerticalLines.RemoveAt(VerticalLines.Count - 1);
                    ammoutLeft--;
                    break;
                }
            }
        }
    }

    public (Vector2 initialPosition, Vector2 finalPosition) CalculateLinePosition(
        int index,
        LineOrientation orientation,
        float spacing
    )
    {
        Vector2 initialPosition;
        Vector2 finalPosition;

        switch (orientation)
        {
            case LineOrientation.Horizontal:
                float horizontalOffset = (index - (HorizontalLines.Count - 1) / 2f) * spacing; // Divisão por 2f para garantir precisão
                initialPosition =
                    (Vector2)linePlacer.position + new Vector2(-Size.x / 2, horizontalOffset);
                finalPosition =
                    (Vector2)linePlacer.position + new Vector2(Size.x / 2, horizontalOffset);
                break;

            case LineOrientation.Vertical:
                float verticalOffset = (index - (VerticalLines.Count - 1) / 2f) * spacing; // Divisão por 2f para garantir precisão
                initialPosition =
                    (Vector2)linePlacer.position + new Vector2(verticalOffset, Size.y / 2);
                finalPosition =
                    (Vector2)linePlacer.position + new Vector2(verticalOffset, -Size.y / 2);
                break;

            default:
                Debug.LogError("Invalid line orientation type!");
                return (Vector2.zero, Vector2.zero);
        }

        return (initialPosition, finalPosition);
    }

    public void OnScrollDirectly()
    {
        float value = ScrollBar.value;
        Spacing = Mathf.Lerp(MIN_SPACING, MAX_SPACING, value);
        return;
    }

    public void CreateDataGraph(Data data)
    {
        GameObject line = new GameObject("custom-line");
        GridLine gl = line.AddComponent<GridLine>();
        gl.Create(this, 0, LineOrientation.Custom, CustomLinePrefab);
        gl.Renderer.sortingOrder = 12;
        gl.Renderer.numCornerVertices = 5;
        Color color = dataMenu.DataColors[GetDataIndex(data)];
        gl.Renderer.startColor = color;
        gl.Renderer.endColor = color;
        DataGraphs.Add(gl);
        line.transform.SetParent(linePlacer);

        Transform parentTransform = dataMenu.LoadedData[data].transform;

        gl.transform.SetParent(parentTransform);
        UpdateDataGraph(data);

        CreateGraphKeyframes(data);
    }

    private void CreateGraphKeyframes(Data data)
    {
        List<GameObject> keys = new List<GameObject>();
        for (int i = 0; i < data.Values.Count; i++)
        {
            GameObject newKeyframe = Instantiate(KeyframePrefab);
            newKeyframe.transform.SetParent(KeyframePlacer);
            newKeyframe.transform.position = Vector3.zero;
            newKeyframe.transform.localPosition = Vector3.zero;
            newKeyframe.GetComponentInChildren<SpriteRenderer>().color = dataMenu.DataColors[
                GetDataIndex(data)
            ];
            keys.Add(newKeyframe);
        }

        Keyframes.Add(data, keys.ToArray());
        UpdateGraphKeyframesColors();
        UpdateGraphKeyframes(data);
    }

    public void DestroyGraphKeyframes(Data data)
    {
        GameObject[] keys = Keyframes[data];
        foreach (GameObject key in keys)
        {
            Destroy(key);
        }
        Keyframes.Remove(data);

        UpdateGraphKeyframesColors();
    }

    private void UpdateGraphKeyframesColors()
    {
        foreach (Data d in dataMenu.dataOrder)
        {
            foreach (GameObject keyframe in Keyframes[d])
            {
                keyframe.GetComponent<SpriteRenderer>().color = dataMenu.DataColors[
                    GetDataIndex(d)
                ];
            }
        }
    }

    private void UpdateGraphKeyframes(Data data)
    {
        // Validação inicial
        if (!Keyframes.ContainsKey(data))
            return;

        GameObject[] keys = Keyframes[data];
        int index = 0;

        float initialPosition = GetInitialLine(LineOrientation.Vertical).Renderer.GetPosition(0).x;
        float finalPosition = GetFinalLine(LineOrientation.Vertical).Renderer.GetPosition(0).x;
        if (Format == FormatMode.Default)
        {
            foreach (GameObject keyframe in keys)
            {
                if (keyframe == null)
                    continue;

                Vector3 position = Vector3.zero;

                float worldMinPosition =
                    initialPosition
                    + GetInitialLine(LineOrientation.Vertical).transform.parent.position.x;
                float worldMaxPosition =
                    finalPosition
                    + GetFinalLine(LineOrientation.Vertical).transform.parent.position.x;

                float localMinPosition = worldMinPosition - transform.position.x * 2;
                float localMaxPosition = worldMaxPosition - transform.position.x * 2;

                // Calcular tempo e porcentagem
                float point_time = data.GetTime(index);
                float total_time = dataMenu.MaxDataTime;

                if (total_time == 0)
                    total_time = 1; // Evitar divisão por zero
                float percentage = point_time / total_time;

                position.x = Mathf.Lerp(localMinPosition, localMaxPosition, percentage);

                // Atualizar posição do keyframe
                keyframe.transform.localPosition = position;
                Debug.Log("Keyframe > " + keyframe.transform.localPosition);
                index++;
            }
        }
        else if (Format == FormatMode.Normalized)
        {
            foreach (GameObject keyframe in keys)
            {
                if (keyframe == null)
                    continue;

                Vector3 position = Vector3.zero;

                float iLimit = -Density.x * Spacing / 2;
                float fLimit = Density.x * Spacing / 2;

                // Calcular tempo e porcentagem
                float point_time = data.GetTime(index);
                float total_time = GetGraphTotalTime(data);

                if (total_time == 0)
                    total_time = 1; // Evitar divisão por zero
                float percentage = point_time / total_time;

                float worldMinPosition =
                    initialPosition
                    + GetInitialLine(LineOrientation.Vertical).transform.parent.position.x;
                float worldMaxPosition =
                    finalPosition
                    + GetFinalLine(LineOrientation.Vertical).transform.parent.position.x;

                float localMinPosition = worldMinPosition - transform.position.x * 2;
                float localMaxPosition = worldMaxPosition - transform.position.x * 2;

                position.x = Mathf.Lerp(localMinPosition, localMaxPosition, percentage);

                // Atualizar posição do keyframe
                keyframe.transform.localPosition = position;
                Debug.Log("Keyframe > " + keyframe.transform.localPosition);
                index++;
            }
        }
        Debug.LogWarning("Keyframes updated");
    }

    GridLine GetLineOfDataDisplayer(Data data)
    {
        return dataMenu.LoadedData[data].GetComponentInChildren<GridLine>();
    }

    private float GetMaxDataTime()
    {
        float max_time = 0;
        foreach (Data data in dataMenu.dataOrder)
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

    void UpdateDataGraph(Data data)
    {
        // Obtém a linha associada ao data
        GridLine line = GetLineOfDataDisplayer(data);
        Vector2 initialPosition = new Vector2(
            GetInitialLine(LineOrientation.Vertical).Renderer.GetPosition(0).x,
            GetInitialLine(LineOrientation.Horizontal).Renderer.GetPosition(0).y
        );

        Vector2 finalPosition = new Vector2(
            GetFinalLine(LineOrientation.Vertical).Renderer.GetPosition(0).x,
            GetFinalLine(LineOrientation.Horizontal).Renderer.GetPosition(0).y
        );

        // Calcula as posições com base nos dados


        // Define o número de posições da linha

        // Obtém o maior valor para a escala global

        // Loop para definir as posições
        //float value = data.GetValue(i);
        if (Format == FormatMode.Default)
        {
            List<float> values = data.Values.Select(x => x.Item2).ToList();
            // Garante que o número de posições do line renderer seja igual ao número de pontos dos dados
            line.Renderer.positionCount = data.Values.Count;

            // Calcula o maior valor e o tempo máximo global fora do loop
            float biggerValue = GetBiggerValue(values);
            float maxTime = dataMenu.MaxDataTime; // O maior tempo global entre todos os gráficos

            // Itera sobre os pontos do gráfico
            for (int index = 0; index < data.Values.Count; index++)
            {
                float time = data.Values[index].Item1; // Tempo atual do ponto
                float value = values[index]; // Valor atual do ponto

                // Calcula a posição no eixo X, escalada pelo tempo máximo global
                float xPosition =
                    initialPosition.x
                    + (time / maxTime * Size.x)
                        * (Mathf.Abs(finalPosition.x - initialPosition.x) / Size.x);

                // Calcula a posição no eixo Y, normalizada pelo maior valor
                float yPosition =
                    initialPosition.y
                    + (value / biggerValue)
                        * Size.y
                        * (Mathf.Abs(finalPosition.y - initialPosition.y) / Size.y);
                ;

                // Define a posição no LineRenderer
                line.Renderer.SetPosition(index, new Vector2(xPosition, yPosition));
            }
        }
        else if (Format == FormatMode.Normalized)
        {
            List<float> values = CalculateGridPositions(data, Format);
            line.Renderer.positionCount = (int)Density.x;
            for (int i = 0; i < Density.x; i++)
            {
                float value = values[i];

                float biggerValue = GetBiggerValue(values); // Ajuste importante!

                line.Renderer.SetPosition(
                    i,
                    new Vector2(
                        initialPosition.x + i * Spacing,
                        initialPosition.y + (value / biggerValue * (Density.y - 1) * Spacing)
                    )
                );
            }
        }
        /*
        line.Renderer.SetPosition(
            line.Renderer.positionCount - 1,
            line.Renderer.GetPosition(line.Renderer.positionCount - 2)
        );
        */
    }

    float GetBiggerValue(List<float> list)
    {
        float max_value = 0;
        foreach (float value in list)
        {
            if (value > max_value)
                max_value = value;
        }
        return max_value;
    }

    float GetGraphTimeInterval(Data data)
    {
        List<(float time, float value)> dataValues = data.GetValues();
        float total_time = GetGraphTotalTime(data);
        float time_interval = total_time / Density.x;
        return time_interval;
    }

    public float GetGraphTotalTime(Data data)
    {
        List<(float time, float value)> dataValues = data.GetValues();
        float total_time = dataValues[dataValues.Count - 1].time;
        return total_time;
    }

    List<float> CalculateGridPositions(Data data, FormatMode mode)
    {
        List<(float time, float value)> dataValues = data.GetValues();
        float total_time = dataValues[dataValues.Count - 1].time;
        float time_interval = GetGraphTimeInterval(data);

        List<float> interpolatedValues = new List<float>();

        for (float time = 0; time <= total_time; time += time_interval)
        {
            // Encontrar os pontos anterior e posterior mais próximos
            (float time, float value) prevPoint = (0, 0);
            (float time, float value) nextPoint = (total_time, 0);

            for (int i = 0; i < dataValues.Count; i++)
            {
                if (dataValues[i].time <= time)
                    prevPoint = dataValues[i];
                else
                {
                    nextPoint = dataValues[i];
                    break;
                }
            }

            // Interpolação linear entre os pontos
            float t = (time - prevPoint.time) / (nextPoint.time - prevPoint.time);
            float interpolatedValue = Mathf.Lerp(prevPoint.value, nextPoint.value, t);

            interpolatedValues.Add(interpolatedValue);
        }

        return interpolatedValues;
    }

    public GridLine GetDataCustomLine(Data data)
    {
        if (GetDataIndex(data) < 0)
        {
            Debug.LogError("Data not found in data menu!");
            return null;
        }
        return DataGraphs[GetDataIndex(data)];
    }

    public int GetDataIndex(Data data)
    {
        // Verifica se o dicionário contém a chave 'data'
        if (dataMenu.dataOrder.Contains(data))
        {
            // Retorna o índice da chave encontrada
            var index = dataMenu.dataOrder.ToList().IndexOf(data);
            return index;
        }

        Debug.LogError("Data not found in data menu!");
        return -1;
    }

    GridLine GetInitialLine(LineOrientation orientation)
    {
        float min_value = float.MaxValue;
        GridLine result = null;
        switch (orientation)
        {
            case LineOrientation.Horizontal:
                foreach (GridLine line in HorizontalLines)
                {
                    if (line.Renderer.GetPosition(0).y < min_value)
                    {
                        min_value = line.Renderer.GetPosition(0).y;
                        result = line;
                    }
                }
                break;
            case LineOrientation.Vertical:
                foreach (GridLine line in VerticalLines)
                {
                    if (line.Renderer.GetPosition(0).x < min_value)
                    {
                        min_value = line.Renderer.GetPosition(0).x;
                        result = line;
                    }
                }
                break;

            case LineOrientation.Custom:
                Debug.LogError("You can't get the initial line of a type CUSTOM line!");
                return null;

            default:
                Debug.LogError("Invalid line orientation type!");
                return null;
        }
        result.Renderer.startColor = Color.white;
        result.Renderer.endColor = Color.white;
        return result;
    }

    GridLine GetFinalLine(LineOrientation orientation)
    {
        float max_value = float.MinValue;
        GridLine result = null;
        switch (orientation)
        {
            case LineOrientation.Horizontal:
                foreach (GridLine line in HorizontalLines)
                {
                    if (line.Renderer.GetPosition(0).y > max_value)
                    {
                        max_value = line.Renderer.GetPosition(0).y;
                        result = line;
                    }
                }
                break;
            case LineOrientation.Vertical:
                foreach (GridLine line in VerticalLines)
                {
                    if (line.Renderer.GetPosition(0).x > max_value)
                    {
                        max_value = line.Renderer.GetPosition(0).x;
                        result = line;
                    }
                }
                break;

            case LineOrientation.Custom:
                Debug.LogError("You can't get the initial line of a type CUSTOM line!");
                return null;

            default:
                Debug.LogError("Invalid line orientation type!");
                return null;
        }
        return result;
    }

    private void UpdateTextPosition(TMP_Text text, Vector2 position)
    {
        text.transform.position = position;
    }
}
