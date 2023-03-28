using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movement : MonoBehaviour
{
    public float speed = 4f;
    public float maxVelocityChange = 10f;

    private Vector2 input;
    private Rigidbody2D rb;

    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    
    void Update()
    {
        input = new Vector2(Input.GetAxisRaw("Horizontal"), 0);
        input.Normalize();
    }
}
