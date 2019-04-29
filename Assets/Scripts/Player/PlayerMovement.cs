﻿using System;
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

    void Start() {
        _animator = GetComponent<Animator>();
        _controller = GetComponent<CharacterController>();
    }

    void Update() {
        if (Input.GetKey(KeyCode.LeftShift)) {
            Dash();
        } else {
            Move();
        }
    }

    void Move() {
        if (Input.GetAxis("Vertical") != 0 || Input.GetAxis("Horizontal") != 0)
        {
            _animator.SetBool("isRunning", true);
            _moveDirection = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
            transform.forward = _moveDirection;
            _moveDirection *= speed;
            _controller.SimpleMove(_moveDirection);
        }
        else if (Input.GetAxis("Vertical") == 0)
        {
            _animator.SetBool("isRunning", false);
        }
    }

    void Dash() {
        if (Input.GetKeyDown(KeyCode.A)) {
            transform.Translate(Vector3.left * dashSpeed);
        } else if (Input.GetKeyDown(KeyCode.D)) {
            transform.Translate(Vector3.right * dashSpeed);
        } else if (Input.GetKeyDown(KeyCode.S)) {
            transform.Translate(Vector3.back * dashSpeed);
        }
    }
}
