using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This is an example of how to assign a puzzle image to a puzzle model at the start of a scene.
/// You will want to create something more sophisticated to suit your needs, as I expect you'll 
/// want to use more than just the three example images I've provided.
/// </summary>
[ExecuteInEditMode]
public class ImageAssigner : MonoBehaviour
{
    [SerializeField] Texture m_landscapeImageTexture;
    [SerializeField] Texture m_portraitImageTexture;
    [SerializeField] Texture m_squareImageTexture;


    private Texture m_imageTexture;


    public string ImageName
    {
        get
        {
            if (m_imageTexture == null)
                AssignImageTexture();

            return m_imageTexture.name;
        }
    }


    void Awake()
    {
        AssignImageTexture();
    }



    private void AssignImageTexture()
    {
        var puzzleManager = (PuzzleManager) FindObjectOfType(typeof(PuzzleManager));

        string puzzleName = puzzleManager.name.ToLower();

        if (puzzleName.Contains("landscape"))
            m_imageTexture = m_landscapeImageTexture;
        else if (puzzleName.Contains("portrait"))
            m_imageTexture = m_portraitImageTexture;
        else
            m_imageTexture = m_squareImageTexture;

        puzzleManager.AssignImageTexture(m_imageTexture);
    }
}
