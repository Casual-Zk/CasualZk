using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScrollingBackground : MonoBehaviour
{
    //[SerializeField] [Range(0, 0.025f)] float localSpeed;
    //[SerializeField] Renderer bgRenderer;
    [SerializeField] RawImage image;
    [SerializeField] float xSpeed, ySpeed;

    //public float speed;

    // Update is called once per frame
    void Update()
    {
        //bgRenderer.material.mainTextureOffset += new Vector2(speed * localSpeed * Time.deltaTime, 0);
        image.uvRect = new Rect(
            image.uvRect.position + new Vector2(xSpeed, ySpeed) * Time.deltaTime,
            image.uvRect.size
        );
    }
}
