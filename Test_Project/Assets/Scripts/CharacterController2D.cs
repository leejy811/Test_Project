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
    [Range(0, 1)] [SerializeField] private float m_CrouchSpeed = .36f;			//앉았을때의 속도 1 = 100% 이다
    [Range(0, 3)] [SerializeField] private float m_DashSpeed = 1.5f;            //대쉬할때의 속도 1 = 100% 이다
    [Range(0, 1)] [SerializeField] private float m_JumpSpeed = 0.625f;
	[Range(0, .3f)] [SerializeField] private float m_MovementSmoothing = .05f;	//움직임이 얼마나 부드러운지에 대한 변수
	[SerializeField] private bool m_AirControl = false;							//공중에서 플레이어를 움직일 수 있는가
	[SerializeField] private LayerMask m_WhatIsGround;							// A mask determining what is ground to the character
	[SerializeField] private Transform m_GroundCheck;							// A position marking where to check if the player is grounded.
	[SerializeField] private Transform m_CeilingCheck;                          // A position marking where to check for ceilings
    [SerializeField] private Collider2D m_CrouchDisableCollider;				// A collider that will be disabled when crouching

	const float k_GroundedRadius = .2f; // Radius of the overlap circle to determine if grounded
	private bool m_Grounded;            // Whether or not the player is grounded.
	const float k_CeilingRadius = .2f; // Radius of the overlap circle to determine if the player can stand up
	private Rigidbody2D m_Rigidbody2D;
	private bool m_FacingRight = true;  // For determining which way the player is currently facing.
    private bool m_JumpFacingRight;
    private Vector3 m_Velocity = Vector3.zero;
    private int m_JumpCount = 2;

    [Header("Events")]
	[Space]

	public UnityEvent OnLandEvent;

	[System.Serializable]
	public class BoolEvent : UnityEvent<bool> { }

	public BoolEvent OnCrouchEvent;
	private bool m_wasCrouching = false;

	private void Awake()
	{
		m_Rigidbody2D = GetComponent<Rigidbody2D>();

		if (OnLandEvent == null)
			OnLandEvent = new UnityEvent();

		if (OnCrouchEvent == null)
			OnCrouchEvent = new BoolEvent();

        m_JumpCount = 0;
	}

	private void FixedUpdate()
	{
		bool wasGrounded = m_Grounded;
		m_Grounded = false;

		// The player is grounded if a circlecast to the groundcheck position hits anything designated as ground
		// This can be done using layers instead but Sample Assets will not overwrite your project settings.
		Collider2D[] colliders = Physics2D.OverlapCircleAll(m_GroundCheck.position, k_GroundedRadius, m_WhatIsGround);
		for (int i = 0; i < colliders.Length; i++)
		{
			if (colliders[i].gameObject != gameObject)
			{
				m_Grounded = true;
                m_JumpCount = 2;
				if (!wasGrounded)
					OnLandEvent.Invoke();
			}
		}
	}


	public void Move(float move, bool crouch, bool jump, bool dash)
	{
		// If crouching, check to see if the character can stand up
		if (!crouch)
		{
			// If the character has a ceiling preventing them from standing up, keep them crouching
			if (Physics2D.OverlapCircle(m_CeilingCheck.position, k_CeilingRadius, m_WhatIsGround))
			{
				crouch = true;
			}
		}

        //only control the player if grounded or airControl is turned on
        if (m_Grounded || m_AirControl)
		{

			// If crouching
			if (crouch && m_Grounded)
			{
				if (!m_wasCrouching)
				{
					m_wasCrouching = true;
					OnCrouchEvent.Invoke(true);
				}

				// Reduce the speed by the crouchSpeed multiplier
				move *= m_CrouchSpeed;

				// Disable one of the colliders when crouching
				if (m_CrouchDisableCollider != null)
					m_CrouchDisableCollider.enabled = false;
			} else
			{
				// Enable the collider when not crouching
				if (m_CrouchDisableCollider != null)
					m_CrouchDisableCollider.enabled = true;

				if (m_wasCrouching)
				{
					m_wasCrouching = false;
					OnCrouchEvent.Invoke(false);
				}
			}

            if (dash && m_Grounded && !crouch)
            {
                move *= m_DashSpeed;
            }

            if((!m_Grounded && move < 0 && m_JumpFacingRight) || (!m_Grounded && move > 0 && !m_JumpFacingRight))
            {
                move *= m_JumpSpeed;
            }
            
			// Move the character by finding the target velocity
			Vector3 targetVelocity = new Vector2(move * 10f, m_Rigidbody2D.velocity.y);
			// And then smoothing it out and applying it to the character
			m_Rigidbody2D.velocity = Vector3.SmoothDamp(m_Rigidbody2D.velocity, targetVelocity, ref m_Velocity, m_MovementSmoothing);

			// If the input is moving the player right and the player is facing left...
			if (move > 0 && !m_FacingRight)
			{
				// ... flip the player.
				Flip();
			}
			// Otherwise if the input is moving the player left and the player is facing right...
			else if (move < 0 && m_FacingRight)
			{
				// ... flip the player.
				Flip();
			}
		}

        // If the player should jump...
        if ((m_Grounded && jump) || (m_JumpCount > 0 && jump))
        {
            // Add a vertical force to the player.
            m_Grounded = false;
            if (m_JumpCount == 2)
            {
                m_Rigidbody2D.velocity = new Vector2(m_Rigidbody2D.velocity.x, m_JumpForce);
                if (move > 0)
                {
                    m_JumpFacingRight = true;
                }
                else if (move < 0)
                {
                    m_JumpFacingRight = false;
                }
            }
            else if(m_JumpCount == 1)
            {
                if (m_FacingRight)
                {
                    m_Rigidbody2D.AddForce(new Vector2(1, 0) * m_JumpDashForce, ForceMode2D.Impulse);
                }
                else if (!m_FacingRight)
                {
                    m_Rigidbody2D.AddForce(new Vector2(-1, 0) * m_JumpDashForce, ForceMode2D.Impulse);
                }
                m_Rigidbody2D.velocity = new Vector2(m_Rigidbody2D.velocity.x, 0);
            }
            m_JumpCount--;
        }
    }


	private void Flip()
	{
		// Switch the way the player is labelled as facing.
		m_FacingRight = !m_FacingRight;

		// Multiply the player's x local scale by -1.
		Vector3 theScale = transform.localScale;
		theScale.x *= -1;
		transform.localScale = theScale;
	}
}