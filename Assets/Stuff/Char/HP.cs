using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class HP : MonoBehaviour
{
    public int hPPlayer;
    public TextMeshProUGUI PlayerHPTextField;

    private void Update()
    {
        CheckIfPlayerDead();
    }

    private void CheckIfPlayerDead()
    {
        if (hPPlayer <= 0)
        {
            PlayerHPTextField.text = "" + hPPlayer;
            Destroy(gameObject);
            Debug.Log("Player is Dead");
        }
        else
        {
            PlayerHPTextField.text = "" + hPPlayer;
        }
    }

    public int HPPlayer
    {
        get { return hPPlayer; }
        set { hPPlayer = value; }
    }

}
