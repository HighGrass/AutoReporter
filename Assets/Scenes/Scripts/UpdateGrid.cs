using System.Linq;
using DataSpace;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class UpdateGrid : MonoBehaviour
{
    DataMenu dataMenu;
    Grid grid;

    public void Start()
    {
        dataMenu = FindObjectOfType<DataMenu>();
        grid = FindObjectOfType<Grid>();
    }
}
