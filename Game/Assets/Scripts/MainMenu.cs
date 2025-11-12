using System.Collections;
using System.Collections.Generic;
using Photon.Pun.UtilityScripts;
using UnityEngine;

public class MainMenu : MonoBehaviour
{
    public GameObject Buttons;
  
    public GameObject Canvas;
    public void OnClickPlay(){
        Buttons.SetActive(false);
        Canvas.SetActive(true);
    }
    public void onClickQuit(){
        Application.Quit();
    }
}
