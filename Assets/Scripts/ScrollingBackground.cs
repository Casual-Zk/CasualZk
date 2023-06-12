using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScrollingBackground : MonoBehaviour
{
    [SerializeField] [Range(0, 0.025f)] float localSpeed;
    [SerializeField] Renderer bgRenderer;

    public float speed;

    // Update is called once per frame
    void Update()
    {
        bgRenderer.material.mainTextureOffset += new Vector2(speed * localSpeed * Time.deltaTime, 0);
    }
}
