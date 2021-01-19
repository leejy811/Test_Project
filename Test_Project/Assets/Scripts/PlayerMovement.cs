using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour {

	public CharacterController2D controller;
	public Animator animator;

	public float runSpeed = 40f;

	float horizontalMove = 0f;
	bool jump = false;
	bool crouch = false;
    bool dash = false;
    bool slide = false;

	// Update is called once per frame
	void Update () {

		horizontalMove = Input.GetAxisRaw("Horizontal") * runSpeed;

		animator.SetFloat("Speed", Mathf.Abs(horizontalMove));

		if (Input.GetButtonDown("Jump"))
		{
			jump = true;
			animator.SetBool("IsJumping", true);
		}

        if (!dash) {
            if (Input.GetButtonDown("Crouch"))
            {
                crouch = true;
            } else if (Input.GetButtonUp("Crouch"))
            {
                crouch = false;
                if (slide)
                {
                    slide = false;
                    dash = true;
                }
            }
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
                if (slide)
                {
                    slide = false;
                }
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
                slide = false;
            }
        }
    }

	public void OnLanding ()
	{
		animator.SetBool("IsJumping", false);
	}

    public void Onair()
    {
        animator.SetBool("IsJumping", true);
    }

	public void OnCrouching (bool isCrouching)
	{
		animator.SetBool("IsCrouching", isCrouching);
	}

	void FixedUpdate ()
	{
		// Move our character
		controller.Move(horizontalMove * Time.fixedDeltaTime, crouch, jump, dash, slide);
		jump = false;
	}
}
