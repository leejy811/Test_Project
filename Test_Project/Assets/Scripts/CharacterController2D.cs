using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* CharacterController2D
 * 해당 스크립트는 플레이어 이동에 대한 커맨드를 입력받고 입력 받은 것을
 * 연산을 통하여 플레이어의 다음 이동을 결정하는 스크립트이다.
 */

public class CharacterController2D : MonoBehaviour
{
    //힘을 주기 위한 힘의 양을 나타내는 변수들
	[SerializeField] private float m_JumpForce = 13f;                           //플레이어의 점프하는 힘
    [SerializeField] private float m_JumpDashForce = 25f;                       //플레이어의 이단 점프하는 힘
    [SerializeField] private float m_DashToJumpForce = 4f;                      //플레이어의 대쉬중 점프하는 힘(원래 힘에 더하는 양

    //쿨타임이나 최대 시간에 대한 변수들
    [SerializeField] private float m_TimeToDashJump = 0.75f;                    //대쉬점프 하는데 필요한 시간
    [SerializeField] private float m_maxSlideTime = 1.5f;                       //최대 슬라이딩 시간
    [SerializeField] private float m_maxSlideCool = 1.5f;                       //슬라이딩 쿨타임

    //플레이어의 속도에 관한 변수 1 = 100%이다
    [Range(0, 1)] [SerializeField] private float m_CrouchSpeed = .36f;			//앉았을때의 속도
    [Range(0, 1)] [SerializeField] private float m_JumpSpeed = 0.625f;          //공중에서의 속도
    [Range(0, 3)] [SerializeField] private float m_DashSpeed = 1.5f;            //대쉬할때의 속도
    [Range(0, 3)] [SerializeField] private float m_SlideSpeed = 2f;             //슬라이딩할때의 속도

    [Range(0, .3f)] [SerializeField] private float m_MovementSmoothing = .05f;	//움직임이 얼마나 부드러운지에 대한 변수

    //땅인지를 체크하거나 위에 천장이 있는지를 체크하는 변수들
    [SerializeField] private LayerMask m_WhatIsGround;                          //땅인지를 레이어를 체크하는데 필요한 변수
	[SerializeField] private Transform m_GroundCheck;                           //땅인지를 체크하는 지점
	[SerializeField] private Transform m_CeilingCheck;                          //위에 천장이 있는지 체크하는 변수
    [SerializeField] private Collider2D m_CrouchDisableCollider;                //앉기를 했을때 없어지는 콜라이더
    [SerializeField] private bool AirControl = true;                            //공중에서 움직일 수 있는지를 체크하는 변수
    private bool m_Grounded;                                                    //현재 땅에 있는지를 체크하는 변수
    private bool m_Celling = false;                                             //현재 위에 천장이 있는지를 체크하는 변수
    private bool m_wasCrouching = false;                                        //앉기 중이었는지 체크하는 변수
    const float k_GroundedRadius = .2f;                                         //땅인지를 체크하는 반경
	const float k_CeilingRadius = .2f;                                          //위에 천장이 있는지를 체크하는 반경

    //플레이어가 어디를 보고 있는지를 판단하는 변수들(true가 오른쪽 false가 왼쪽)
    private bool m_FacingRight = true;                                          //플레이어가 어디를 보고 있는지를 판단하는 변수
    private bool m_JumpFacingRight;                                             //점프를 할때 플레이어가 어디를 보고 있는지를 판단하는 변수

    private Vector3 m_Velocity = Vector3.zero;                                  //smoothDamp함수에 이용할 zero Vector3를 저장하는 변수
    private int m_JumpCount = 2;                                                //최대 점프 카운트
    private Rigidbody2D m_Rigidbody2D;                                          //물리적인 연산을 처리하는 Rigidbody2D 컴포넌트 변수

    //시간을 재는 변수들
    private float m_TimeToDash = 0f;                                            //대쉬를 한지 얼마나 됐는지 시간을 재는 변수
    private float m_TimeToSlide = 0f;                                           //슬라이딩을 한지 얼마나 됐는지 시간을 재는 변수
    private float m_SlideCool = 10f;                                            //슬라이딩 쿨타임을 재는 변수

    //PlayerMovement에 있던 변수들
    public Animator animator;                                                   //애니메이션을 사용하기 위한 Animator 컴포넌트 변수
    public float runSpeed = 40f;                                                //플레이어가 달리는 속도

    [SerializeField] float horizontalMove = 0f;                                 //플레이어가 이동하는 방향을 입력받는 변수
    [SerializeField] bool jump = false;                                         //플레이어가 점프 중인지를 판단하는 변수
    [SerializeField] bool crouch = false;                                       //플레이어가 앉기 중인지를 판단하는 변수
    [SerializeField] bool dash = false;                                         //플레이어가 대쉬 중인지를 판단하는 변수
    [SerializeField] bool slide = false;                                        //플레이어가 슬라이딩 중인지를 판단하는 변수


    //초기 변수들을 초기화 하기위한 Awake 함수 선언
    private void Awake()
	{
		m_Rigidbody2D = GetComponent<Rigidbody2D>();        //m_Rigidbody2D 변수를 초기화

        m_JumpCount = 0;        //m_JumpCount변수를 0으로 초기화
    }

    //매 프레임 실행되는 Update 함수 선언
    void Update()
    {
        //플레이어의 이동을 입력받고 애니메이션을 동작시킴
        horizontalMove = Input.GetAxisRaw("Horizontal") * runSpeed;
        animator.SetFloat("Speed", Mathf.Abs(horizontalMove));

        //점프키를 입력받고 애니메이션을 동작시킴
        if (Input.GetButtonDown("Jump") && !crouch && !slide && !m_Celling)
        {
            jump = true;
            animator.SetBool("IsJumping", true);
        }

        //앉기 상태가 아닐때 대쉬를 입력받는다
        if (!crouch)
        {
            //입력키는 왼쪽 Shift키
            if (Input.GetKeyDown(KeyCode.LeftShift))
                dash = true;
            else if (Input.GetKeyUp(KeyCode.LeftShift))
                dash = false;
        }

        //대쉬 상태인 경우 앉기 키를 눌러 슬라이드를 입력받는다
        if (dash)
        {
            if (Input.GetButtonDown("Crouch"))
                slide = true;
            else if (Input.GetButtonUp("Crouch"))
            {
                //슬라이딩 중에 앉기 키를 떼는 경우 -> 슬라이드 취소 대쉬키가 실행 쿨타임 초기화
                if (slide)
                {
                    slide = false;
                    m_SlideCool = 0f;       //슬라이딩 쿨타임을 초기화
                }
            }
        }
        //대쉬 상태는 아닌데 슬라이드가 켜져있는경우(슬라이딩 중에 대쉬키를 떼는 경우)
        // -> 슬라이드 취소 앉기 키로 변경 쿨타임 초기화
        else if (slide)
        {
            crouch = true;
            slide = false;
            m_SlideCool = 0f;       //슬라이딩 쿨타임을 초기화
        }
        //대쉬 상태도 아니고 슬라이드도 아닌 상태에서 앉기를 입력받는 경우
        else
        {
            //Crouch키로 앉기를 활성화, 비활성화
            if (Input.GetButtonDown("Crouch"))
                crouch = true;
            else if (Input.GetButtonUp("Crouch"))
                crouch = false;
        }
    }

    //고정된 분당 50프레임 중에 매프레임 동작하는 FixedUpdate 선언(물리적 동작 포함)
    private void FixedUpdate()
	{
        //현재 땅에 있는지를 wasGrounded에 넣고 m_Grounded를 false로 초기화
        bool wasGrounded = m_Grounded;
		m_Grounded = false;

        //플레이어의 밑바닥 주위로 있는 플레이어를 제외한 모든 콜라이더를 colliders에 넣는다
        Collider2D[] colliders = Physics2D.OverlapCircleAll(m_GroundCheck.position, k_GroundedRadius, m_WhatIsGround);
		for (int i = 0; i < colliders.Length; i++)
		{
            //콜라이더가 플레이어가 아니라면
			if (colliders[i].gameObject != gameObject)
			{
				m_Grounded = true;          //현재 땅에 있음
                m_JumpCount = 2;            //점프 카운트를 초기화
                
                //그 이전에 땅에 있지 않았다면 점프 애니메이션 취소
                if (!wasGrounded)
                    animator.SetBool("IsJumping", false);
            }
        }

        //만약 현재 땅에 있지 않다면 점프 애니메이션 실행
        if (!m_Grounded)
            animator.SetBool("IsJumping", true);

        Move(horizontalMove * Time.fixedDeltaTime, crouch);         //플레이어의 물리적 동작을 총괄하는 함수 Move 호출
        jump = false;       //점프를 한번 실행 했으니 취소
    }

    //플레이어의 물리적 동작을 총괄하는 함수 Move 선언
    void Move(float move, bool isCrouch)
    {
        //만약 땅에 닿았다면 점프 하는 힘을 원래대로 초기화
        if (m_Grounded && m_JumpForce != 17f)
            m_JumpForce -= m_DashToJumpForce;

        //만약 공중 조작이 불가능한 상환에서 땅에 닿았거나 앉기키가 눌리지 않았다면 공중조작이 가능해진다.
        if ((m_Grounded || !isCrouch) && !AirControl)
            AirControl = true;

        //슬라이드 상태가 아니거나 멈춰있는 상태면
        if (!slide || move == 0)
        {
            m_SlideCool += Time.fixedDeltaTime;         //쿨타임을 재기 시작한다.
            m_TimeToSlide = 0f;                         //슬라이딩 하는 시간을 초기화

            //슬라이딩 도중에 방향키를 떼는 경우 -> 슬라이드 취소 앉기 키로 변경 쿨타임 초기화
            if (slide)
            {
                slide = false;
                crouch = true;
                m_SlideCool = 0f;
            }
        }

        //현재 대쉬키가 눌렸는데 플레이어가 움직이지 않는경우
        if (dash && move == 0)
        {
            dash = false;               //대쉬 취소
            m_TimeToDash = 0f;          //대쉬 하는 시간을 초기화
        }

        //앉기 상태가 아니고 슬라이딩 상태가 아닐때 천장에 무언가가 있다면 앉기 상태로 변경
        if (!isCrouch && !slide)
            if (Physics2D.OverlapCircle(m_CeilingCheck.position, k_CeilingRadius, m_WhatIsGround))
            {
                m_Celling = true;
                isCrouch = true;
            }
            else
                m_Celling = false;
        else
            m_Celling = false;

        if (m_Grounded || AirControl)
        {
            move *= ImpleCrouch(move, isCrouch);        //앉기를 구현하는 함수 ImpleCrouch를 호출해 move를 변경

            move *= ImpleSlide(move);                   //슬라이딩를 구현하는 함수 ImpleSlide를 호출해 move를 변경

            move *= ImpleDash(move, isCrouch);          //대쉬를 구현하는 함수 ImpleDash를 호출해 move를 변경

            //플레이어가 점프한 방향의 반대 방향으로 움직인다면 느린 속도로 움직임
            if ((!m_Grounded && move < 0 && m_JumpFacingRight) || (!m_Grounded && move > 0 && !m_JumpFacingRight))
                move *= m_JumpSpeed;

            //플레이어가 원래의 속도에서 targetVelocity의 속도로 속도가 점차 증가함
            Vector3 targetVelocity = new Vector2(move * 10f, m_Rigidbody2D.velocity.y);
            m_Rigidbody2D.velocity = Vector3.SmoothDamp(m_Rigidbody2D.velocity, targetVelocity, ref m_Velocity, m_MovementSmoothing);

            //플레이어가 어딜 바라보냐에 따라 방향을 바꾸는 Flip함수 호출
            if (move > 0 && !m_FacingRight)
                Flip();
            else if (move < 0 && m_FacingRight)
                Flip();
        }

        ImpleJump(move);        //점프를 구현하는 함수 ImpleJump를 호출
    }

    //방향을 바꾸는 Flip함수 선언
    private void Flip()
	{
		m_FacingRight = !m_FacingRight;                 //현재 m_FacingRight을 반대로 바꾼다

        //플레이어의 scale을 -1을 곱해 반대 방향으로 돌림
        Vector3 theScale = transform.localScale;
		theScale.x *= -1;
		transform.localScale = theScale;
	}

    //앉기를 구현하는 함수 ImpleCrouch를 선언
    private float ImpleCrouch(float move, bool isCrouch)
    {
        //앉기 상태고 땅에 있는 경우
        if (isCrouch && m_Grounded)
        {
            //이전에 앉기 상태가 아니였다면 애니메이션을 작동하고 true상태로 바꿈
            if (!m_wasCrouching)
            {
                m_wasCrouching = true;
                animator.SetBool("IsCrouching", true);
            }

            //앉기 상태가 되어 위의 콜라이더를 false 상태로 만듬
            if (m_CrouchDisableCollider != null)
                m_CrouchDisableCollider.enabled = false;

            return m_CrouchSpeed;       //m_CrouchSpeed를 반환(move를 m_CrouchSpeed로 바꿈)
        }
        //공중에서 앉기 키를 누른경우(공중에서 웅크리기 기능)
        else if(isCrouch && !m_Grounded)
        {
            //이전에 앉기 상태였다면 애니메이션을 취소하고 false상태로 바꿈
            if (m_wasCrouching)
            {
                m_wasCrouching = false;
                animator.SetBool("IsCrouching", false);
            }

            //앉기 상태가 되어 위의 콜라이더를 false 상태로 만듬
            if (m_CrouchDisableCollider != null)
                m_CrouchDisableCollider.enabled = false;

            AirControl = false;             //공중에서 이동 불가

            return 1f;      //1을 반환(move를 그대로 유지)
        }
        //아닌 경우
        else
        {
            //앉기 상태가 되어 위의 콜라이더 true 상태로 만듬
            if (m_CrouchDisableCollider != null)
                m_CrouchDisableCollider.enabled = true;

            //이전에 앉기 상태였다면 애니메이션을 취소하고 false상태로 바꿈
            if (m_wasCrouching)
            {
                m_wasCrouching = false;
                animator.SetBool("IsCrouching", false);
            }

            return 1f;      //1을 반환(move를 그대로 유지)
        }
    }

    //슬라이딩을 구현하는 함수 ImpleSlide를 호출
    private float ImpleSlide(float move)
    {
        //슬라이드 상태고 땅에 있는 경우
        if (slide && m_Grounded)
        {
            //만약 최대 슬라이딩 시간을 초과했다면
            if (m_TimeToSlide > m_maxSlideTime)
            {
                //위의 콜라이더를 true 상태로 바꿈
                if (m_CrouchDisableCollider != null)
                    m_CrouchDisableCollider.enabled = true;

                //앉기 상태로 돌아가고 쿨타임을 초기화
                slide = false;
                dash = false;
                crouch = true;
                m_TimeToSlide = 0f;
            }
            //만약 쿨타임이 끝났다면
            else if (m_SlideCool > m_maxSlideCool)
            {
                m_TimeToSlide += Time.fixedDeltaTime;       //슬라이딩하는 시간을 재기 시작

                //위의 콜라이더를 flase 상태로 바꿈
                if (m_CrouchDisableCollider != null)
                    m_CrouchDisableCollider.enabled = false;

                return m_SlideSpeed;        //m_SlideSpeed를 반환(move를 m_SlideSpeed로 바꿈)
            }
            //쿨타임이 아직 되지 않았다면 slide를 false 상태로 바꿈
            else if (m_SlideCool < m_maxSlideCool)
                slide = false;
        }

        return 1f;          //1을 반환(move를 그대로 유지)
    }

    //대쉬를 구현하는 함수 ImpleDash를 호출
    private float ImpleDash(float move, bool isCrouch)
    {
        //대쉬의 조건을 만족하면(대쉬 상태, 땅에 접촉, 앉기 상태 아님, 슬라이딩 상태 아님)
        if (dash && m_Grounded && !isCrouch && !slide)
        {
            m_TimeToDash += Time.fixedDeltaTime;        //대쉬하는 시간을 재기 시작

            //만약 대쉬하는 시간이 대쉬 점프에 필요한 시간을 넘기면 점프를 강화
            if (m_TimeToDash > m_TimeToDashJump)
                m_JumpForce += m_DashToJumpForce;

            return m_DashSpeed;         //m_DashSpeed를 반환(move를 m_DashSpeed로 바꿈)
        }
        //대쉬를 안하는 상태라면
        else if (!dash)
        {
            m_TimeToDash = 0f;          //대쉬하는 시간을 초기화
            //만약 점프하는 힘이 원래 힘이 아니라면 힘을 약화
            if (m_JumpForce != 17f)
                m_JumpForce -= m_DashToJumpForce;
        }

        return 1f;          //1을 반환(move를 그대로 유지)
    }

    //점프를 구현하는 함수 ImpleJump를 호출
    private void ImpleJump(float move)
    {
        //땅에닿아있거나 점프를 한번한 상태에서 점프키가 눌리고
        if ((m_Grounded || m_JumpCount == 1) && jump)
        {
            m_Grounded = false;         //땅에 있는 상태가 아님
            //만약 점프를 한번한 상태라면
            if (m_JumpCount == 2)
            {
                //캐릭터의 y축 속도 변경
                m_Rigidbody2D.velocity = new Vector2(m_Rigidbody2D.velocity.x, m_JumpForce);

                //어디로 점프했냐에 따라 봤던 방향 결정
                if (move > 0)
                    m_JumpFacingRight = true;
                else if (move < 0)
                    m_JumpFacingRight = false;

                m_TimeToDash = 0f;          //대쉬점프를 위해 재던 시간 초기화
            }
            //만약 점프를 두번한 상태라면
            else if (m_JumpCount == 1)
            {
                //바라보는 방향으로 공중대쉬를 함(AddForce로 구현)
                if (m_FacingRight)
                    m_Rigidbody2D.AddForce(Vector2.right * m_JumpDashForce, ForceMode2D.Impulse);
                else if (!m_FacingRight)
                    m_Rigidbody2D.AddForce(Vector2.left * m_JumpDashForce, ForceMode2D.Impulse);

                //위로 더이상 올라가지 않음
                m_Rigidbody2D.velocity = new Vector2(m_Rigidbody2D.velocity.x, 0);
            }
            m_JumpCount--;          //점프 카운트를 센다.
        }
    }
}