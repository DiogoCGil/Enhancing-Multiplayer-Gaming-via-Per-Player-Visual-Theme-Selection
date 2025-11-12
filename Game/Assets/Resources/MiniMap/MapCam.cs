using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapCam : MonoBehaviour
{
    public Transform target;

    void Update()
    {
        if (target != null)
        {
            // Seguir a posição do jogador, mantendo a altura fixa
            transform.position = new Vector3(target.position.x, transform.position.y, target.position.z);

            // Ajustar a rotação da câmera do minimapa para coincidir com a do jogador
            transform.rotation = Quaternion.Euler(90f, target.eulerAngles.y, 0f);
        }
    }
}
