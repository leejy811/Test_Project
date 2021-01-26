using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour {

	public CharacterController2D controller;
	public Animator animator;

	public float runSpeed = 40f;

    [SerializeField] float horizontalMove = 0f;
    [SerializeField] bool jump = false;
    [SerializeField] bool crouch = false;
    [SerializeField] bool dash = false;
    [SerializeField] bool slide = false;

	// Update is called once per frame
	void Update () {

		horizontalMove = Input.GetAxisRaw("Horizontal") * runSpeed;

		animator.SetFloat("Speed", Mathf.Abs(horizontalMove));

		if (Input.GetButtonDown("Jump"))
		{
			jump = true;
			animator.SetBool("IsJumping", true);
		}

        if (!crouch)
        {
            if (Input.GetKeyDown(KeyCode.LeftShift))
            {
                dash = true;
            }
            else if (Input.GetKeyUp(KeyCode.LeftShift))
            {
                dash = false;
            }
        }

        if (dash)
        {
            if (Input.GetButtonDown("Crouch"))
            {
                slide = true;
            }
            else if (Input.GetButtonUp("Crouch"))
            {
                if (slide)
                {
                    slide = false;      //슬라이딩 중에 앉기 키를 떼는 경우
                    controller.ResetSlideCool();
                }
            }
        }
        else if (slide)
        {
            //슬라이딩 중에 대쉬 키를 떼는 경우
            crouch = true;
            slide = false;
            controller.ResetSlideCool();
        }
        else
        {
            if (Input.GetButtonDown("Crouch"))
            {
                crouch = true;
            }
            else if (Input.GetButtonUp("Crouch"))
            {
                crouch = false;
            }
        }
    }

    void FixedUpdate()
    {
        // Move our character
        controller.Move(horizontalMove * Time.fixedDeltaTime, crouch, jump, dash, slide);
        jump = false;
    }

    public void OnLanding ()
	{
		animator.SetBool("IsJumping", false);
	}

    public void OnAir()
    {
        animator.SetBool("IsJumping", true);
    }

	public void OnCrouchAnim (bool isCrouching)
	{
		animator.SetBool("IsCrouching", isCrouching);
	}

    public void OnCrouching(bool isCrouching)
    {
        crouch = isCrouching ? true : false;
    }

    public void OnSliding(bool isSliding)
    {
        slide = isSliding ? true : false;
    }

    public void OnDashing(bool isDashing)
    {
        dash = isDashing ? true : false;
    }
}
