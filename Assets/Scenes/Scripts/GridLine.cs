using UnityEngine;
using UnityEngine.UIElements;

public class GridLine : MonoBehaviour
{
    public Grid Grid { get; private set; }
    public int GridIndex { get; private set; }
    public LineRenderer Renderer { get; set; }
    public LineOrientation Orientation { get; private set; }
    public bool Visible { get; private set; } = true;

    public void SetOrientation(LineOrientation orientation) => Orientation = orientation;

    public void Create(Grid grid, int index, LineOrientation orientation, GameObject linePrefab)
    {
        Grid = grid;
        GridIndex = index;
        Orientation = orientation;
        GameObject newObj = Instantiate(linePrefab);
        newObj.transform.SetParent(transform);
        SetRenderer(newObj.GetComponent<LineRenderer>());
    }

    private void SetRenderer(LineRenderer render) => Renderer = render;

    public void SetIndex(int index) => GridIndex = index;

    public void Show()
    {
        Renderer.enabled = true;
        Visible = true;
    }

    public void Hide()
    {
        Renderer.enabled = false;
        Visible = false;
    }
}
