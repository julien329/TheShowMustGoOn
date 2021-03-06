﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerMovement : MonoBehaviour {

    ////////////////////////////////////////////////////////////////////////////////////////////////
    /// VARIABLES
    ////////////////////////////////////////////////////////////////////////////////////////////////

    public Vector3 Velocity { get { return velocity; } }
    public AudioClip[] cheerSounds;
    public float speed = 4f;                   
    public float reducedSpeedMultiplier = 0.95f;
    public float gravity = -9.8f;
    public float accelerationTime = 0.05f;
    public float rotationTime = 0.05f;

    private Vector3 viewDirection;
    private Animator anim;                      // Reference to the animator component.
	private Rigidbody playerRigidbody;          // Reference to the player's rigidbody.
    private Vector3 velocity;
    private AudioSource audioSource;
    private float activeVelocityXSmoothing;
    private float activeVelocityYSmoothing;


    ////////////////////////////////////////////////////////////////////////////////////////////////
    /// UNITY
    ////////////////////////////////////////////////////////////////////////////////////////////////

    void Awake() {
		anim = GetComponent<Animator>();
		playerRigidbody = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
    }


	void Update() {
        Move();
        Animating();
    }


    void FixedUpdate() {
        playerRigidbody.AddForce(new Vector3(0, gravity, 0), ForceMode.Acceleration);
    }


    /////////////////////////////////////////////////////////////////////////////////////////////////
    /// METHODS
    ////////////////////////////////////////////////////////////////////////////////////////////////

    void Move() {
        // Get input value of left joystick
        float h = Input.GetAxis("LeftHorizontal");
        float v = Input.GetAxis("LeftVertical");

        // Set the movement vector based on the player input.
        velocity.x = Mathf.SmoothDamp(velocity.x, h, ref activeVelocityXSmoothing, accelerationTime);
        velocity.z = Mathf.SmoothDamp(velocity.z, v, ref activeVelocityYSmoothing, accelerationTime);
        velocity = Vector3.ClampMagnitude(velocity, 1.0f);

        Rotate();

        // Move current position to target position, smoothed and scaled by speed
        playerRigidbody.MovePosition(transform.position + velocity * speed * Time.deltaTime);
	}


    void Rotate() {
        // Get input value of right joystick
        float h = Input.GetAxis("RightHorizontal");
        float v = Input.GetAxis("RightVertical");

        // Set looking direction of the player
        viewDirection.Set(h, 0f, v);
        if (viewDirection == Vector3.zero) {
            viewDirection = velocity;
        }
        else {
            viewDirection = Vector3.ClampMagnitude(viewDirection, 1.0f);
            velocity *= reducedSpeedMultiplier;
        }

        // Change rotation of the player
        if (viewDirection != Vector3.zero) {
            Quaternion targetRotation = Quaternion.LookRotation(viewDirection, Vector3.up);
            Quaternion newRotation = Quaternion.Lerp(playerRigidbody.rotation, targetRotation, rotationTime * Time.deltaTime);
            playerRigidbody.MoveRotation(newRotation);
        }
    }


    void Animating() {
		Vector3 cameraDirection = Vector3.Normalize(Camera.main.transform.forward);
		cameraDirection.y = 0f;

		// Where the player will move
		Vector3 movement_dir = new Vector3 (velocity.x, 0f, velocity.z);

        // Calculate mouvement direction
        float angle = Vector3.Angle(transform.forward, cameraDirection.normalized);
        angle = (Vector3.Angle(transform.right, cameraDirection.normalized) > 90f) ? 360f - angle : angle;
        movement_dir = Quaternion.AngleAxis (angle , Vector3.up) * movement_dir;
	
		//translated movement direction
		anim.SetFloat("Horizontal", movement_dir.x, 0.15f, Time.deltaTime);
		anim.SetFloat("Vertical", movement_dir.z, 0.15f, Time.deltaTime);
	}


	public void StartAttackAnim() {
		anim.SetTrigger ("Attack");
        audioSource.clip = cheerSounds[Random.Range(0, cheerSounds.Length)];
        audioSource.Play();
    }
}


