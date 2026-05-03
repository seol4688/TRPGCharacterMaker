using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;

public class DataManager
{
    public void Init()
    {
        //TextAsset textAsset = Resources.Load<TextAsset>($"Data/DNDData");
        //Debug.Log(textAsset);
        //RaceData data = JsonUtility.FromJson<RaceData>(textAsset.text);
        //DNDData();
    }

    #region D&D
    public Dictionary<string, DND.Race> DNDRaceDict { get; private set; } = new Dictionary<string, DND.Race>();

    void DNDData()
    {
        DNDRaceDict = LoadJson<DND.RaceData, string, DND.Race>("DNDData").MackDict();
        Debug.Log(DNDRaceDict);
    }
    #endregion

    #region Insane
    private const string InsaneDirectoryName = "Insane";
    private const string InsaneCharacterSheetDirectoryName = "CharacterSheet";
    private const string InsaneCustomSheetDirectoryName = "CustomSpecialty";
    private const string DefaultInsaneCharacterFileName = "insane-character.json";
    private const string DefaultInsaneCustomSheetFileName = "insane-custom-sheet.json";

    public void SaveInsaneCharacterSheet(InsaneManager insaneManager, string jsonName = "")
    {
        if (insaneManager == null)
        {
            return;
        }

        SaveInsaneCharacterSheet(insaneManager.CreateCharacterSheetSaveData(), jsonName);
    }

    public void SaveInsaneCharacterSheet(InsaneCharacterSheet characterSheet, string jsonName = "")
    {
        if (characterSheet == null)
        {
            return;
        }

        WriteJson(GetInsaneSavePath(jsonName, DefaultInsaneCharacterFileName), characterSheet);
    }

    public void LoadInsaneCharacterSheet(InsaneManager insaneManager, string jsonName = "")
    {
        if (insaneManager == null)
        {
            return;
        }

        insaneManager.ApplyCharacterSheetSaveData(LoadInsaneCharacterSheet(jsonName));
    }

    public InsaneCharacterSheet LoadInsaneCharacterSheet(string jsonName = "")
    {
        return ReadJson<InsaneCharacterSheet>(GetInsaneSavePath(jsonName, DefaultInsaneCharacterFileName));
    }

    public string[] GetInsaneCharacterSheetSaveNames()
    {
        return GetJsonFileNames(Path.Combine(Application.persistentDataPath, InsaneDirectoryName, InsaneCharacterSheetDirectoryName));
    }

    public void DeleteInsaneCharacterSheet(string jsonName = "")
    {
        string path = GetInsaneSavePath(jsonName, DefaultInsaneCharacterFileName);
        if (File.Exists(path))
            File.Delete(path);
    }

    public DateTime? GetInsaneCharacterSheetCreationTime(string jsonName = "")
    {
        string path = GetInsaneSavePath(jsonName, DefaultInsaneCharacterFileName);
        return File.Exists(path) ? File.GetCreationTime(path) : (DateTime?)null;
    }

    public void SaveInsaneCustomSheet(CustomSheetSaveData customSheetSaveData, string jsonName = "")
    {
        if (customSheetSaveData == null)
        {
            return;
        }

        WriteJson(GetInsaneCustomSheetSavePath(jsonName, DefaultInsaneCustomSheetFileName), customSheetSaveData);
    }

    public CustomSheetSaveData LoadInsaneCustomSheet(string jsonName = "")
    {
        return ReadJson<CustomSheetSaveData>(GetInsaneCustomSheetSavePath(jsonName, DefaultInsaneCustomSheetFileName));
    }

    public string[] GetInsaneCustomSheetSaveNames()
    {
        return GetJsonFileNames(Path.Combine(Application.persistentDataPath, InsaneDirectoryName, InsaneCustomSheetDirectoryName));
    }

    public void DeleteInsaneCustomSheet(string jsonName = "")
    {
        string path = GetInsaneCustomSheetSavePath(jsonName, DefaultInsaneCustomSheetFileName);
        if (File.Exists(path))
            File.Delete(path);
    }

    public bool GetInsaneCustomSheetFavorite(string jsonName = "")
    {
        CustomSheetSaveData data = LoadInsaneCustomSheet(jsonName);
        return data != null && data.isFavorite;
    }

    public void SetInsaneCustomSheetFavorite(string jsonName, bool isFavorite)
    {
        CustomSheetSaveData data = LoadInsaneCustomSheet(jsonName) ?? new CustomSheetSaveData();
        data.isFavorite = isFavorite;
        SaveInsaneCustomSheet(data, jsonName);
    }
    #endregion

    Loader LoadJson<Loader, Key, Value>(string path) where Loader : ILoader<Key, Value>
    {
        TextAsset textAsset = Resources.Load<TextAsset>($"Data/{path}");
        Debug.Log(textAsset);
        return JsonUtility.FromJson<Loader>(textAsset.text);
    }

    private void WriteJson<T>(string savePath, T data)
    {
        string saveDirectory = Path.GetDirectoryName(savePath);
        if (!string.IsNullOrEmpty(saveDirectory))
        {
            Directory.CreateDirectory(saveDirectory);
        }

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(savePath, json);
    }

    private T ReadJson<T>(string savePath) where T : class
    {
        if (!File.Exists(savePath))
        {
            return null;
        }

        string json = File.ReadAllText(savePath);
        return JsonUtility.FromJson<T>(json);
    }

    private string GetInsaneSavePath(string jsonName, string defaultFileName)
    {
        return Path.Combine(Application.persistentDataPath, InsaneDirectoryName, InsaneCharacterSheetDirectoryName, NormalizeJsonFileName(jsonName, defaultFileName));
    }

    private string GetInsaneCustomSheetSavePath(string jsonName, string defaultFileName)
    {
        return Path.Combine(Application.persistentDataPath, InsaneDirectoryName, InsaneCustomSheetDirectoryName, NormalizeJsonFileName(jsonName, defaultFileName));
    }

    private string[] GetJsonFileNames(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            return Array.Empty<string>();
        }

        string[] filePaths = Directory.GetFiles(directoryPath, "*.json", SearchOption.TopDirectoryOnly);
        string[] fileNames = new string[filePaths.Length];

        for (int i = 0; i < filePaths.Length; i++)
        {
            fileNames[i] = Path.GetFileNameWithoutExtension(filePaths[i]);
        }

        Array.Sort(fileNames, StringComparer.OrdinalIgnoreCase);
        return fileNames;
    }

    private string NormalizeJsonFileName(string fileName, string defaultFileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return defaultFileName;
        }

        string safeName = Path.GetFileName(fileName.Trim());
        char[] invalidChars = Path.GetInvalidFileNameChars();

        for (int i = 0; i < invalidChars.Length; i++)
        {
            safeName = safeName.Replace(invalidChars[i], '_');
        }

        if (string.IsNullOrWhiteSpace(safeName))
        {
            return defaultFileName;
        }

        return EnsureJsonExtension(safeName);
    }

    private string EnsureJsonExtension(string fileName)
    {
        return Path.GetExtension(fileName).Equals(".json", StringComparison.OrdinalIgnoreCase)
            ? fileName
            : $"{fileName}.json";
    }
}

public interface ILoader<Key, Value>
{
    Dictionary<Key, Value> MackDict();
}

