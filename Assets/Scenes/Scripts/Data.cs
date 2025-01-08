using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace DataSpace
{
    public class Data
    {
        public enum Type
        {
            SIMPLE,
            COMPLEX,
        }

        public string Name { get; set; } = "DataDisplay";
        private string defaultFilePath = Application.dataPath + "/StreamingAssets/DataFiles/";
        public List<(float, float)> Values { get; private set; }
        public Type DataType { get; private set; }

        public Data(string name, string data = null, Type dataType = Type.SIMPLE)
        {
            Name = name;
            Values = new List<(float, float)>();
            DataType = dataType;

            if (data != null)
            {
                RewriteData(data);
                SaveDataInDevice();
            }

            Application.quitting += OnApplicationQuit;
        }

        public List<(float, float)> GetValues() => Values;

        public float GetTime(int index) => GetValues()[index].Item1;

        public float GetValue(int index) => GetValues()[index].Item2;

        public void RewriteData(string data)
        {
            Values = ReadData(data);
        }

        public void Feed((float time, (float v1, float v2) value) data)
        {
            if (data.value.v2 == 0 && DataType == Type.SIMPLE)
            {
                Debug.LogWarning("Data fed with COMPLEX data, but data type is SIMPLE");
            }
            Values.Add((data.time, ProcessData(data.value)));
        }

        private float ProcessData((float, float) data)
        {
            switch (DataType)
            {
                case Type.SIMPLE:
                    return data.Item1;

                case Type.COMPLEX:
                    return data.Item1 - data.Item2;

                default:
                    Debug.LogError("ERROR - Invalid data type input");
                    return 0;
            }
        }

        private List<(float, float)> ReadData(string data)
        {
            string[] dataLines = data.Split('\n');
            Debug.Log("File has " + dataLines.Length + " lines");

            var newData = new List<(float, float)>();

            foreach (string line in dataLines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                string[] lineData = line.Split('|');
                Debug.Log("Data || Time: " + lineData[0] + " | Value: " + lineData[1]);

                // Ensure the data is correctly formatted with decimal points
                float time = float.Parse(lineData[0], CultureInfo.InvariantCulture);
                float value = float.Parse(lineData[1], CultureInfo.InvariantCulture);

                (float, float) dataPoint = (time, value);

                newData.Add(dataPoint);
            }

            return newData;
        }

        private string WriteData(List<(float, float)> data)
        {
            string newData = "";

            foreach (var point in data)
            {
                newData +=
                    $"{point.Item1.ToString(CultureInfo.InvariantCulture)}|{point.Item2.ToString(CultureInfo.InvariantCulture)}\n";
            }

            return newData;
        }

        public void SaveDataInDevice()
        {
            if (!FolderExists(defaultFilePath))
                CreateFolder(defaultFilePath);

            string filePath = defaultFilePath + Name + ".txt";

            if (!FileExists(filePath))
            {
                CreateFile(filePath, WriteData(Values));
            }
            else
            {
                EditFile(filePath, WriteData(Values));
            }
        }

        private void OnApplicationQuit()
        {
            SaveData();
        }

        private void SaveData()
        {
            SaveDataInDevice();
        }

        private bool FolderExists(string path) => Directory.Exists(path);

        private void CreateFolder(string path) => Directory.CreateDirectory(path);

        private bool FileExists(string path) => File.Exists(path);

        private void CreateFile(string path, string content) => File.WriteAllText(path, content);

        private void EditFile(string path, string content) => File.WriteAllText(path, content);
    }
}
