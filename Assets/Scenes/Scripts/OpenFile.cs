using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using DataSpace;
using SFB;
using UnityEngine;
using UnityEngine.Networking;

public class OpenFile : MonoBehaviour
{
    DataMenu dataMenu;
    private string defaultFilePath;

    public void Start()
    {
        defaultFilePath = Application.dataPath + "/StreamingAssets/DataFiles/";
        dataMenu = FindAnyObjectByType<DataMenu>();
    }

    public void ChooseFile()
    {
        string[] paths = StandaloneFileBrowser.OpenFilePanel(
            "Open File",
            defaultFilePath,
            "txt",
            false
        );
        if (paths.Length > 0 && dataMenu.LoadedData.Count < dataMenu.MAX_DATA_FILES)
        {
            StartCoroutine(OutputRoutine(paths[0]));
        }
    }

    private IEnumerator OutputRoutine(string path)
    {
        UnityWebRequest request = UnityWebRequest.Get(path);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Request error: " + request.error);
        }
        else
        {
            string[] pathD = path.Split('\\');
            string[] pathDD = pathD[pathD.Length - 1].Split('/');
            string fileName = pathDD[pathDD.Length - 1].Replace(".txt", "");
            Debug.Log("FileName: " + fileName);
            Debug.Log("File open successfully: " + request.downloadHandler.text);
            dataMenu.AddData(new Data(fileName, request.downloadHandler.text));
        }
    }
}
