using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using valvy;

public class KickHammer : MonoBehaviour
{
    public ValvyView valvyview;

    void OnCollisionEnter(Collision collision)
    {
        if (valvyview.IsMine)
        {
            return;
        }
        else
        {
            Application.Quit();
        }
    }
}
