using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 10.0f;
    public float rotateSpeed = 300.0f;
    public float dashSpeed = 50.0f;
    public float gravity = 20.0f;

    private CharacterController _controller;
    private float _rotation;
    private Vector3 _moveDirection = Vector3.zero;

    void Start()
    {
        _controller = GetComponent<CharacterController>();
    }

    void Update()
    {   
        if (Input.GetKey(KeyCode.LeftShift))
        {
            Dash();
        }
        else
        {
            Move();
        }

        _moveDirection.y -= gravity * Time.deltaTime;
    }

    void Move()
    {
        _rotation = Input.GetAxis("Horizontal") * rotateSpeed * Time.deltaTime;
        transform.Rotate(0, _rotation, 0);
        
        _moveDirection = Vector3.forward * Input.GetAxis("Vertical");
        _moveDirection = transform.TransformDirection(_moveDirection);
        _moveDirection *= speed;

        _controller.Move(_moveDirection * Time.deltaTime);
    }

    void Dash()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            transform.Translate(Vector3.left * dashSpeed);
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {
            transform.Translate(Vector3.right * dashSpeed);
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            transform.Translate(Vector3.back * dashSpeed);
        }
    }
}
