using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Events;

public class CheatCode : MonoBehaviour
{
    [SerializeField] bool m_allowCheatCode = true;
    [SerializeField] bool m_multipleUse = false;
    [Tooltip("Letters or space, no numbers or other characters")]
    [SerializeField] string m_cheatCode = "complete";
    [SerializeField] UnityEvent m_event;

    private KeyCode[] m_code;
    private int m_index;
    private bool m_triggered;

    private void Awake()
    {
        var characters = m_cheatCode.ToCharArray();

        m_code = new KeyCode[characters.Length];

        for (int i = 0; i < characters.Length; i++)
        {
            string character = characters[i].ToString().ToUpper();

            if (Enum.TryParse(character, out KeyCode keyCode))
                m_code[i] = keyCode;
            else if (character == " ")
                m_code[i] = KeyCode.Space;
        }
    }


    void Update()
    {
        // Add in an extra check for a global property to easily disable all cheat codes
        if (m_triggered || !m_allowCheatCode) // || !GlobalConfig.AllowCheatCodes)
            return;

        if (Input.anyKeyDown)
        {
            if (Input.GetKeyDown(m_code[m_index]))
            {
                m_index++;
            }
            else
                m_index = 0;

            if (m_index == m_code.Length)
            {
                if (m_event != null)
                    m_event.Invoke();

                if (!m_multipleUse)
                    m_triggered = true;

                m_index = 0;
            }
        }
    }


    public void Reset()
    {
        m_triggered = false;
    }
}