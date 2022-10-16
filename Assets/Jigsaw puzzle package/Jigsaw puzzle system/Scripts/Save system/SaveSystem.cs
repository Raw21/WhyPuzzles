using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public class SaveSystem : MonoBehaviour
{
    public static void SavePuzzlePieceData(PuzzlePiecesData puzzlePieceData)
    {
        string path = string.Format("{0}/{1}.dat", Application.persistentDataPath, puzzlePieceData.name);

        var formatter = new BinaryFormatter();

        using (var stream = new FileStream(path, FileMode.Create))
        {
            formatter.Serialize(stream, puzzlePieceData);
        }
    }


    public static PuzzlePiecesData LoadPuzzlePiecesData(string puzzleName)
    {
        string path = string.Format("{0}/{1}.dat", Application.persistentDataPath, puzzleName);

        PuzzlePiecesData puzzlePiecesData = null;

        if (File.Exists(path))
        {
            var formatter = new BinaryFormatter();

            using (var stream = new FileStream(path, FileMode.Open))
            {
                puzzlePiecesData = formatter.Deserialize(stream) as PuzzlePiecesData;
            }
        }

        return puzzlePiecesData;
    }


    public static void DeleteSaveData(string puzzleName)
    {
        string path = string.Format("{0}/{1}.dat", Application.persistentDataPath, puzzleName);

        if (File.Exists(path))
            File.Delete(path);
    }
}
