using System;
//using System.Diagnostics;
using System.IO;
using UnityEngine;

public class FileHandler
{
    public bool FileExists(string path) => File.Exists(path);

    public bool FolderExists(string path) => Directory.Exists(path);

    public static void CreateFile(string path, string content)
    {
        try
        {
            File.WriteAllText(path, content);
            Debug.Log("Arquivo salvo com sucesso: " + Path.GetFullPath(path));
        }
        catch (Exception ex)
        {
            Debug.LogError(ex.Message);
        }
    }

    public static void EditFile(string path, string content)
    {
        //File.AppendAllText(path, content + Environment.NewLine);
        try
        {
            File.WriteAllText(path, content);
            Debug.Log("Arquivo salvo com sucesso: " + Path.GetFullPath(path));
        }
        catch (Exception ex)
        {
            Debug.LogError(ex.Message);
        }
    }

    public static void ReadFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                string content = File.ReadAllText(path);
            }
            else
            {
                Debug.LogError("Aquivo não encontrado.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Erro ao ler o arquivo: {ex.Message}");
        }
    }

    public static void CreateFolder(string path)
    {
        try
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                Debug.Log("Folder created successfully: " + Path.GetFullPath(path));
            }
            else
            {
                Debug.LogError("Folder already exists: " + Path.GetFullPath(path));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao criar a pasta: {ex.Message}");
        }
    }
}
