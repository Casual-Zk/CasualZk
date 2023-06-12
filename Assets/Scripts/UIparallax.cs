using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIparallax : MonoBehaviour
{
    [SerializeField] RawImage image;
    [SerializeField] [Range(0, 0.03f)] float speed;

    void Update()
    {
        image.uvRect = new Rect(image.uvRect.position + new Vector2(speed, 0) * Time.deltaTime, image.uvRect.size);
    }
}
