using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour {
    public float speed = 10.0f;
    public float rotateSpeed = 300.0f;
    public float dashSpeed = 50.0f;
    public float gravity = 20.0f;

    private Animator _animator;
    private CharacterController _controller;
    private float _rotation;
    private Vector3 _moveDirection = Vector3.zero;

    void Start()
    {
        _animator = GetComponent<Animator>();
        _controller = GetComponent<CharacterController>();

        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        Animate();

        if (Input.GetKey(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
        }

        if (Input.GetKey(KeyCode.LeftShift)) {
            Dash();
        } else {
            Move();
        }
    }

    void Move()
    {
        if (Input.GetAxis("Vertical") != 0 || Input.GetAxis("Horizontal") != 0)
        {
            _rotation = Input.GetAxis("Horizontal") * rotateSpeed;
            transform.Rotate(0, _rotation, 0);

            _moveDirection = Vector3.forward * Input.GetAxis("Vertical");
            _moveDirection = transform.TransformDirection(_moveDirection);
            _moveDirection *= speed;

            _controller.SimpleMove(_moveDirection);
        }
    }

    void Dash()
    {
        if (Input.GetKeyDown(KeyCode.A)) {
            transform.Translate(Vector3.left * dashSpeed);
        } else if (Input.GetKeyDown(KeyCode.D)) {
            transform.Translate(Vector3.right * dashSpeed);
        } else if (Input.GetKeyDown(KeyCode.S)) {
            transform.Translate(Vector3.back * dashSpeed);
        }
    }

    void Animate()
    {
        if (Input.GetAxis("Vertical") > 0)
        {
            _animator.SetBool("isRunning", true);
            _animator.SetBool("isWalking", false);
        }

        if (Input.GetAxis("Vertical") < 0)
        {
            _animator.SetBool("isWalking", true);
            _animator.SetBool("isRunning", false);
        }

        if (Input.GetAxis("Vertical") == 0)
        {
            _animator.SetBool("isRunning", false);
            _animator.SetBool("isWalking", false);
        }
    }
}
