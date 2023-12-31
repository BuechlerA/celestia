﻿using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Transform trans;
    [SerializeField] Transform modelHolder;
    [SerializeField] CharacterController charController;
    [SerializeField] Animator animator;
    [SerializeField] private Transform mainCamera;

    [Header("GUI References")]
    [SerializeField] Image crosshair;

    [Header("Cinemachine References")]
    [SerializeField] CinemachineFreeLook freeLookCamera;
    [SerializeField] CinemachineVirtualCamera aimCamera;

    [Header("Gravity")]
    [SerializeField] float maxGravity = 92;
    [SerializeField] float timeToMaxGravity = .6f;

    private bool isAiming = false;
    private float yVelocity = 0;

    private float GravityPerSecond
    {
        get
        {
            return maxGravity / timeToMaxGravity;
        }
    }

    [Header("Movement")]
    [SerializeField] float movespeed = 42;
    [SerializeField] float turnspeed = 12;
    [SerializeField] float timeToMaxSpeed = .3f;
    [SerializeField] float timeToLoseMaxSpeed = .2f;
    [SerializeField] float reverseMomentumMultiplier = .6f;
    [SerializeField] float midairMovementMultiplier = .4f;
    [SerializeField] float bounciness = .2f;

    private Vector3 localMovementDirection = Vector3.zero;
    private Vector3 worldVelocity = Vector3.zero;
    
    private bool grounded = false;

    private float VelocityGainPerSecond
    {
        get
        {
            if(grounded)
                return movespeed / timeToMaxSpeed;
            else
                return (movespeed / timeToMaxSpeed) * midairMovementMultiplier;
        }
    }

    private float VelocityLossPerSecond
    {
        get
        {
            return movespeed / timeToLoseMaxSpeed;
        }
    }

    [Header("Jumping")]
    [SerializeField] float jumpPower = 76;

    [Header("Wall Jumping")]
    [SerializeField] float wallJumpPower = 40;
    [SerializeField] float wallUpwardsPower = 56;
    [SerializeField] float wallDetectionRange = 2.4f;
    [SerializeField] float wallJumpCooldown = .3f;
    [SerializeField] LayerMask wallDetectionLayerMask;
    
    private float lastWallJumpTime;
    
    private bool WallJumpIsOffCooldown
    {
        get
        {
            return Time.time > lastWallJumpTime + wallJumpCooldown;
        }
    }

    private bool WallIsNearby()
    {
        return Physics.OverlapBox(
            trans.position + Vector3.up * (charController.height * .5f),
            Vector3.one * wallDetectionRange,
            modelHolder.rotation,
            wallDetectionLayerMask.value).Length > 0;
    }

    private void Start()
    {
        crosshair.enabled = false;
    }

    void Movement()
    {
        localMovementDirection = Vector3.zero;

        if (mainCamera == null)
        {
            Debug.LogError("Main Camera not assigned in Player");
            return;
        }

        if (isAiming)
        {
            Quaternion targetRotation = Quaternion.Euler(0f, mainCamera.transform.eulerAngles.y, 0f);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, 10f * Time.deltaTime);
            charController.Move(transform.TransformDirection(localMovementDirection.x, 0f, localMovementDirection.z) * Time.deltaTime * movespeed);
        }

        //Right and left
        if (Input.GetKey(KeyCode.D))
            localMovementDirection += mainCamera.right;
        else if (Input.GetKey(KeyCode.A))
            localMovementDirection -= mainCamera.right;

        //Forward and back
        if (Input.GetKey(KeyCode.W))
            localMovementDirection += mainCamera.forward;
        else if (Input.GetKey(KeyCode.S))
            localMovementDirection -= mainCamera.forward;

        // Make sure movement is not affected by camera's up and down tilt
        localMovementDirection.y = 0;
        localMovementDirection.Normalize();

        if (localMovementDirection != Vector3.zero)
        {
            Vector3 WorldMovementDirection = localMovementDirection;

            float singleStep = turnspeed * Time.deltaTime;
            Vector3 newDirection = Vector3.RotateTowards(modelHolder.forward, WorldMovementDirection, singleStep, 0.0f);
            modelHolder.rotation = Quaternion.LookRotation(newDirection);

            float multiplier = 1;
            float dot = Vector3.Dot(WorldMovementDirection.normalized, worldVelocity.normalized);

            if (dot < 0)
                multiplier += -dot * reverseMomentumMultiplier;

            Vector3 newVelocity = worldVelocity + WorldMovementDirection *
            VelocityGainPerSecond * multiplier * Time.deltaTime;

            if (worldVelocity.magnitude > movespeed)
                worldVelocity = Vector3.ClampMagnitude(newVelocity, worldVelocity.magnitude);
            else
                worldVelocity = Vector3.ClampMagnitude(newVelocity, movespeed);

            // Update isRunning in the animator
            animator.SetBool("isRunning", true);
        }
        else
        {
            animator.SetBool("isRunning", false);
        }
    }

    void Aim()
    {
        //Switch to aim camera when right mouse button is clicked, switch back to free look when it's clicked again
        if (Input.GetMouseButtonDown(1))
        {
            if (isAiming)
            {
                aimCamera.Priority = 9;
                freeLookCamera.Priority = 11;
                isAiming = false;
                crosshair.enabled = false;
            }
            else
            {
                aimCamera.Priority = 11;
                freeLookCamera.Priority = 9;
                isAiming = true;
                crosshair.enabled = true;
            }
        }
    }

    void VelocityLoss()
    {
        if(grounded && (localMovementDirection == Vector3.zero || worldVelocity.magnitude > movespeed))
        {
            float velocityLoss = VelocityLossPerSecond * Time.deltaTime;

            if(velocityLoss > worldVelocity.magnitude)
                worldVelocity = Vector3.zero;
            else    
                worldVelocity -= worldVelocity.normalized * velocityLoss;
        }
    }

    void Gravity()
    {
        if(!grounded && yVelocity > -maxGravity)
            yVelocity = Mathf.Max(yVelocity - GravityPerSecond * Time.
            deltaTime, -maxGravity);
    }

    void WallJumping()
    {
        if(!grounded && WallJumpIsOffCooldown)
        {
            if(Input.GetKeyDown(KeyCode.Space))
            {
                if(WallIsNearby())
                {
                    if(localMovementDirection != Vector3.zero)
                    {
                        worldVelocity = modelHolder.TransformDirection(localMovementDirection) * wallJumpPower;

                        if(yVelocity <= 0)
                            yVelocity = wallUpwardsPower;
                        else
                            yVelocity += wallUpwardsPower;

                        lastWallJumpTime = Time.time;
                    }
                }
            }
        }
    }

    void Jumping()
    {
        if(grounded && Input.GetKeyDown(KeyCode.Space))
        {
            yVelocity = jumpPower;
            grounded = false;

            // Set isJumped in the animator
            animator.SetTrigger("isJumped");
        }
    }

    void ApplyVelocity()
    {
        if(grounded)
            yVelocity = -1;

        Vector3 movementThisFrame = (worldVelocity + (Vector3.up * yVelocity)) * Time.deltaTime;

        if (Input.GetKey(KeyCode.LeftShift))
        {
            movementThisFrame *= 1.75f;
        }
        Vector3 predictedPosition = trans.position + movementThisFrame;

        if(movementThisFrame.magnitude > .03f)
            charController.Move(movementThisFrame);

        //Checking ground state:
        if(!grounded && charController.collisionFlags.HasFlag(CollisionFlags.Below))
            grounded = true;
        else if(grounded && !charController.collisionFlags.HasFlag(CollisionFlags.Below))
            grounded = false;

        //Bounce of walls:
        if(!grounded && charController.collisionFlags.HasFlag(CollisionFlags.Sides))
            worldVelocity = (trans.position - predictedPosition).normalized * (worldVelocity.magnitude *bounciness);
        
        //Lose Y velocity if player goes up and collides with something above
        if(yVelocity > 0 && charController.collisionFlags.HasFlag(CollisionFlags.Above))
            yVelocity = 0;
    }

    
    private void AddVelocity(Vector3 amount)
    {
        worldVelocity += new Vector3(amount.x, 0, amount.z);

        yVelocity += amount.y;

        if(yVelocity > 0)
            grounded = false;
    }
   
    void Update()
    {
        Movement();
        VelocityLoss();
        Gravity();
        WallJumping();
        Jumping();
        Aim();
        ApplyVelocity();
    }
}
