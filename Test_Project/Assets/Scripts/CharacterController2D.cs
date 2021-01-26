using UnityEngine;
using UnityEngine.Events;

/*CharacterController2D
 * 해당 스크립트는 플레이어의 기본적인 물리적 이동에 관하여 실제적인 연산과
 * 판단을 하기위한 스크립트임
*/
public class CharacterController2D : MonoBehaviour
{
	[SerializeField] private float m_JumpForce = 13f;                           //플레이어의 점프하는 힘
    [SerializeField] private float m_JumpDashForce = 25f;
    [SerializeField] private float m_DashToJumpForce = 16f;
    [SerializeField] private float m_TimeToDashJump = 0.75f;
    [SerializeField] private float m_maxSlideTime = 1.5f;
    [SerializeField] private float m_maxSlideCool = 1.5f;
    [Range(0, 1)] [SerializeField] private float m_CrouchSpeed = .36f;			//앉았을때의 속도 1 = 100% 이다
    [Range(0, 1)] [SerializeField] private float m_JumpSpeed = 0.625f;
    [Range(0, 3)] [SerializeField] private float m_DashSpeed = 1.5f;            //대쉬할때의 속도 1 = 100% 이다
    [Range(0, 3)] [SerializeField] private float m_SlideSpeed = 2f;            //슬라이딩할때의 속도 1 = 100% 이다
    [Range(0, .3f)] [SerializeField] private float m_MovementSmoothing = .05f;	//움직임이 얼마나 부드러운지에 대한 변수
	[SerializeField] private bool m_AirControl = false;                         //공중에서 플레이어를 움직일 수 있는가
    [SerializeField] private LayerMask m_WhatIsGround;
	[SerializeField] private Transform m_GroundCheck;
	[SerializeField] private Transform m_CeilingCheck;
    [SerializeField] private Collider2D m_CrouchDisableCollider;

	const float k_GroundedRadius = .2f;
	private bool m_Grounded;
	const float k_CeilingRadius = .2f;
	private Rigidbody2D m_Rigidbody2D;
	private bool m_FacingRight = true;
	private bool m_SlideFacingRight = true;
    private bool m_JumpFacingRight;
    private Vector3 m_Velocity = Vector3.zero;
    private int m_JumpCount = 2;
    private float m_TimeToDash = 0f;
    private float m_TimeToSlide = 0f;
    private float m_SlideCool = 10f;

	private bool m_wasCrouching = false;

    public Animator animator;
    public float runSpeed = 40f;

    [SerializeField] float horizontalMove = 0f;
    [SerializeField] bool jump = false;
    [SerializeField] bool crouch = false;
    [SerializeField] bool dash = false;
    [SerializeField] bool slide = false;

    private void Awake()
	{
		m_Rigidbody2D = GetComponent<Rigidbody2D>();

        m_JumpCount = 0;
	}

    void Update()
    {

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
                dash = true;
            else if (Input.GetKeyUp(KeyCode.LeftShift))
                dash = false;
        }

        if (dash)
        {
            if (Input.GetButtonDown("Crouch"))
                slide = true;
            else if (Input.GetButtonUp("Crouch"))
            {
                if (slide)
                {
                    slide = false;      //슬라이딩 중에 앉기 키를 떼는 경우
                    m_SlideCool = 0f;
                }
            }
        }
        else if (slide)
        {
            crouch = true;
            slide = false;
            m_SlideCool = 0f;
        }
        else
        {
            if (Input.GetButtonDown("Crouch"))
                crouch = true;
            else if (Input.GetButtonUp("Crouch"))
                crouch = false;
        }
    }

    private void FixedUpdate()
	{
		bool wasGrounded = m_Grounded;
		m_Grounded = false;

		Collider2D[] colliders = Physics2D.OverlapCircleAll(m_GroundCheck.position, k_GroundedRadius, m_WhatIsGround);
		for (int i = 0; i < colliders.Length; i++)
		{
			if (colliders[i].gameObject != gameObject)
			{
				m_Grounded = true;
                m_JumpCount = 2;
                if (!wasGrounded)
                    animator.SetBool("IsJumping", false);
            }
        }

        if (!m_Grounded)
            animator.SetBool("IsJumping", true);

        Move(horizontalMove * Time.fixedDeltaTime, crouch);
        jump = false;
    }


    void Move(float move, bool isCrouch)
    {
        if (m_Grounded)
            m_JumpForce = 17f;

        if (!slide || move == 0)
        {
            m_SlideCool += Time.fixedDeltaTime;
            m_TimeToSlide = 0f;

            if (slide)
            {
                //슬라이딩 도중에 방향키를 떼는 경우
                slide = false;
                crouch = true;
                m_SlideCool = 0f;
            }
        }

        if (dash && move == 0)
        {
            dash = false;
            m_TimeToDash = 0f;
        }

        if (!isCrouch && !slide)
            if (Physics2D.OverlapCircle(m_CeilingCheck.position, k_CeilingRadius, m_WhatIsGround))
                isCrouch = true;

        if ((m_Grounded || m_AirControl))
		{
            move *= ImpleCrouch(move, isCrouch);

            move *= ImpleSlide(move);

            move *= ImpleDash(move, isCrouch);

            if ((!m_Grounded && move < 0 && m_JumpFacingRight) || (!m_Grounded && move > 0 && !m_JumpFacingRight))
                move *= m_JumpSpeed;

            Vector3 targetVelocity = new Vector2(move * 10f, m_Rigidbody2D.velocity.y);
			m_Rigidbody2D.velocity = Vector3.SmoothDamp(m_Rigidbody2D.velocity, targetVelocity, ref m_Velocity, m_MovementSmoothing);

			if (move > 0 && !m_FacingRight)
				Flip();
			else if (move < 0 && m_FacingRight)
				Flip();
		}

        ImpleJump(move);
    }

	private void Flip()
	{
		m_FacingRight = !m_FacingRight;

		Vector3 theScale = transform.localScale;
		theScale.x *= -1;
		transform.localScale = theScale;
	}

    private float ImpleCrouch(float move, bool isCrouch)
    {
        if (isCrouch && m_Grounded)
        {
            if (!m_wasCrouching)
            {
                m_wasCrouching = true;
                animator.SetBool("IsCrouching", true);
            }

            if (m_CrouchDisableCollider != null)
                m_CrouchDisableCollider.enabled = false;

            return m_CrouchSpeed;
        }
        else
        {
            if (m_CrouchDisableCollider != null)
                m_CrouchDisableCollider.enabled = true;

            if (m_wasCrouching)
            {
                m_wasCrouching = false;
                animator.SetBool("IsCrouching", false);
            }

            return 1f;
        }
    }

    private float ImpleSlide(float move)
    {
        if (slide && m_Grounded)
        {
            if (m_TimeToSlide > m_maxSlideTime)
            {
                if (m_CrouchDisableCollider != null)
                    m_CrouchDisableCollider.enabled = true;
                slide = false;
                crouch = true;
                m_TimeToSlide = 0f;
            }
            else if (m_SlideCool > m_maxSlideCool)
            {
                if (m_TimeToSlide == 0f)
                    m_SlideFacingRight = move > 0 ? true : false;

                m_TimeToSlide += Time.fixedDeltaTime;

                if (m_CrouchDisableCollider != null)
                    m_CrouchDisableCollider.enabled = false;

                return m_SlideSpeed;
            }
            else if (m_SlideCool < m_maxSlideCool)
                slide = false;
        }

        return 1f;
    }

    private float ImpleDash(float move, bool isCrouch)
    {
        if (dash && m_Grounded && !isCrouch && !slide)
        {
            m_TimeToDash += Time.fixedDeltaTime;

            if (m_TimeToDash > m_TimeToDashJump)
                m_JumpForce = m_DashToJumpForce;

            return m_DashSpeed;
        }
        else if (!dash)
        {
            m_TimeToDash = 0f;
            m_JumpForce = 17f;
        }

        return 1f;
    }

    private void ImpleJump(float move)
    {
        if (((m_Grounded && jump) || (m_JumpCount == 1 && jump)) && !slide)
        {
            m_Grounded = false;
            if (m_JumpCount == 2)
            {
                m_Rigidbody2D.velocity = new Vector2(m_Rigidbody2D.velocity.x, m_JumpForce);
                if (move > 0)
                    m_JumpFacingRight = true;
                else if (move < 0)
                    m_JumpFacingRight = false;
                m_TimeToDash = 0f;
            }
            else if (m_JumpCount == 1)
            {
                if (m_FacingRight)
                    m_Rigidbody2D.AddForce(Vector2.right * m_JumpDashForce, ForceMode2D.Impulse);
                else if (!m_FacingRight)
                    m_Rigidbody2D.AddForce(Vector2.left * m_JumpDashForce, ForceMode2D.Impulse);

                m_Rigidbody2D.velocity = new Vector2(m_Rigidbody2D.velocity.x, 0);
            }
            m_JumpCount--;
        }
    }
}