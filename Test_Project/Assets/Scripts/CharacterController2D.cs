using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* CharacterController2D
 * �ش� ��ũ��Ʈ�� �÷��̾� �̵��� ���� Ŀ�ǵ带 �Է¹ް� �Է� ���� ����
 * ������ ���Ͽ� �÷��̾��� ���� �̵��� �����ϴ� ��ũ��Ʈ�̴�.
 */

public class CharacterController2D : MonoBehaviour
{
    //���� �ֱ� ���� ���� ���� ��Ÿ���� ������
	[SerializeField] private float m_JumpForce = 13f;                           //�÷��̾��� �����ϴ� ��
    [SerializeField] private float m_JumpDashForce = 25f;                       //�÷��̾��� �̴� �����ϴ� ��
    [SerializeField] private float m_DashToJumpForce = 4f;                      //�÷��̾��� �뽬�� �����ϴ� ��(���� ���� ���ϴ� ��

    //��Ÿ���̳� �ִ� �ð��� ���� ������
    [SerializeField] private float m_TimeToDashJump = 0.75f;                    //�뽬���� �ϴµ� �ʿ��� �ð�
    [SerializeField] private float m_maxSlideTime = 1.5f;                       //�ִ� �����̵� �ð�
    [SerializeField] private float m_maxSlideCool = 1.5f;                       //�����̵� ��Ÿ��

    //�÷��̾��� �ӵ��� ���� ���� 1 = 100%�̴�
    [Range(0, 1)] [SerializeField] private float m_CrouchSpeed = .36f;			//�ɾ������� �ӵ�
    [Range(0, 1)] [SerializeField] private float m_JumpSpeed = 0.625f;          //���߿����� �ӵ�
    [Range(0, 3)] [SerializeField] private float m_DashSpeed = 1.5f;            //�뽬�Ҷ��� �ӵ�
    [Range(0, 3)] [SerializeField] private float m_SlideSpeed = 2f;             //�����̵��Ҷ��� �ӵ�

    [Range(0, .3f)] [SerializeField] private float m_MovementSmoothing = .05f;	//�������� �󸶳� �ε巯������ ���� ����

    //�������� üũ�ϰų� ���� õ���� �ִ����� üũ�ϴ� ������
    [SerializeField] private LayerMask m_WhatIsGround;                          //�������� ���̾ üũ�ϴµ� �ʿ��� ����
	[SerializeField] private Transform m_GroundCheck;                           //�������� üũ�ϴ� ����
	[SerializeField] private Transform m_CeilingCheck;                          //���� õ���� �ִ��� üũ�ϴ� ����
    [SerializeField] private Collider2D m_CrouchDisableCollider;                //�ɱ⸦ ������ �������� �ݶ��̴�
    [SerializeField] private bool AirControl = true;                            //���߿��� ������ �� �ִ����� üũ�ϴ� ����
    private bool m_Grounded;                                                    //���� ���� �ִ����� üũ�ϴ� ����
    private bool m_Celling = false;                                             //���� ���� õ���� �ִ����� üũ�ϴ� ����
    private bool m_wasCrouching = false;                                        //�ɱ� ���̾����� üũ�ϴ� ����
    const float k_GroundedRadius = .2f;                                         //�������� üũ�ϴ� �ݰ�
	const float k_CeilingRadius = .2f;                                          //���� õ���� �ִ����� üũ�ϴ� �ݰ�

    //�÷��̾ ��� ���� �ִ����� �Ǵ��ϴ� ������(true�� ������ false�� ����)
    private bool m_FacingRight = true;                                          //�÷��̾ ��� ���� �ִ����� �Ǵ��ϴ� ����
    private bool m_JumpFacingRight;                                             //������ �Ҷ� �÷��̾ ��� ���� �ִ����� �Ǵ��ϴ� ����

    private Vector3 m_Velocity = Vector3.zero;                                  //smoothDamp�Լ��� �̿��� zero Vector3�� �����ϴ� ����
    private int m_JumpCount = 2;                                                //�ִ� ���� ī��Ʈ
    private Rigidbody2D m_Rigidbody2D;                                          //�������� ������ ó���ϴ� Rigidbody2D ������Ʈ ����

    //�ð��� ��� ������
    private float m_TimeToDash = 0f;                                            //�뽬�� ���� �󸶳� �ƴ��� �ð��� ��� ����
    private float m_TimeToSlide = 0f;                                           //�����̵��� ���� �󸶳� �ƴ��� �ð��� ��� ����
    private float m_SlideCool = 10f;                                            //�����̵� ��Ÿ���� ��� ����

    //PlayerMovement�� �ִ� ������
    public Animator animator;                                                   //�ִϸ��̼��� ����ϱ� ���� Animator ������Ʈ ����
    public float runSpeed = 40f;                                                //�÷��̾ �޸��� �ӵ�

    [SerializeField] float horizontalMove = 0f;                                 //�÷��̾ �̵��ϴ� ������ �Է¹޴� ����
    [SerializeField] bool jump = false;                                         //�÷��̾ ���� �������� �Ǵ��ϴ� ����
    [SerializeField] bool crouch = false;                                       //�÷��̾ �ɱ� �������� �Ǵ��ϴ� ����
    [SerializeField] bool dash = false;                                         //�÷��̾ �뽬 �������� �Ǵ��ϴ� ����
    [SerializeField] bool slide = false;                                        //�÷��̾ �����̵� �������� �Ǵ��ϴ� ����


    //�ʱ� �������� �ʱ�ȭ �ϱ����� Awake �Լ� ����
    private void Awake()
	{
		m_Rigidbody2D = GetComponent<Rigidbody2D>();        //m_Rigidbody2D ������ �ʱ�ȭ

        m_JumpCount = 0;        //m_JumpCount������ 0���� �ʱ�ȭ
    }

    //�� ������ ����Ǵ� Update �Լ� ����
    void Update()
    {
        //�÷��̾��� �̵��� �Է¹ް� �ִϸ��̼��� ���۽�Ŵ
        horizontalMove = Input.GetAxisRaw("Horizontal") * runSpeed;
        animator.SetFloat("Speed", Mathf.Abs(horizontalMove));

        //����Ű�� �Է¹ް� �ִϸ��̼��� ���۽�Ŵ
        if (Input.GetButtonDown("Jump") && !crouch && !slide && !m_Celling)
        {
            jump = true;
            animator.SetBool("IsJumping", true);
        }

        //�ɱ� ���°� �ƴҶ� �뽬�� �Է¹޴´�
        if (!crouch)
        {
            //�Է�Ű�� ���� ShiftŰ
            if (Input.GetKeyDown(KeyCode.LeftShift))
                dash = true;
            else if (Input.GetKeyUp(KeyCode.LeftShift))
                dash = false;
        }

        //�뽬 ������ ��� �ɱ� Ű�� ���� �����̵带 �Է¹޴´�
        if (dash)
        {
            if (Input.GetButtonDown("Crouch"))
                slide = true;
            else if (Input.GetButtonUp("Crouch"))
            {
                //�����̵� �߿� �ɱ� Ű�� ���� ��� -> �����̵� ��� �뽬Ű�� ���� ��Ÿ�� �ʱ�ȭ
                if (slide)
                {
                    slide = false;
                    m_SlideCool = 0f;       //�����̵� ��Ÿ���� �ʱ�ȭ
                }
            }
        }
        //�뽬 ���´� �ƴѵ� �����̵尡 �����ִ°��(�����̵� �߿� �뽬Ű�� ���� ���)
        // -> �����̵� ��� �ɱ� Ű�� ���� ��Ÿ�� �ʱ�ȭ
        else if (slide)
        {
            crouch = true;
            slide = false;
            m_SlideCool = 0f;       //�����̵� ��Ÿ���� �ʱ�ȭ
        }
        //�뽬 ���µ� �ƴϰ� �����̵嵵 �ƴ� ���¿��� �ɱ⸦ �Է¹޴� ���
        else
        {
            //CrouchŰ�� �ɱ⸦ Ȱ��ȭ, ��Ȱ��ȭ
            if (Input.GetButtonDown("Crouch"))
                crouch = true;
            else if (Input.GetButtonUp("Crouch"))
                crouch = false;
        }
    }

    //������ �д� 50������ �߿� �������� �����ϴ� FixedUpdate ����(������ ���� ����)
    private void FixedUpdate()
	{
        //���� ���� �ִ����� wasGrounded�� �ְ� m_Grounded�� false�� �ʱ�ȭ
        bool wasGrounded = m_Grounded;
		m_Grounded = false;

        //�÷��̾��� �عٴ� ������ �ִ� �÷��̾ ������ ��� �ݶ��̴��� colliders�� �ִ´�
        Collider2D[] colliders = Physics2D.OverlapCircleAll(m_GroundCheck.position, k_GroundedRadius, m_WhatIsGround);
		for (int i = 0; i < colliders.Length; i++)
		{
            //�ݶ��̴��� �÷��̾ �ƴ϶��
			if (colliders[i].gameObject != gameObject)
			{
				m_Grounded = true;          //���� ���� ����
                m_JumpCount = 2;            //���� ī��Ʈ�� �ʱ�ȭ
                
                //�� ������ ���� ���� �ʾҴٸ� ���� �ִϸ��̼� ���
                if (!wasGrounded)
                    animator.SetBool("IsJumping", false);
            }
        }

        //���� ���� ���� ���� �ʴٸ� ���� �ִϸ��̼� ����
        if (!m_Grounded)
            animator.SetBool("IsJumping", true);

        Move(horizontalMove * Time.fixedDeltaTime, crouch);         //�÷��̾��� ������ ������ �Ѱ��ϴ� �Լ� Move ȣ��
        jump = false;       //������ �ѹ� ���� ������ ���
    }

    //�÷��̾��� ������ ������ �Ѱ��ϴ� �Լ� Move ����
    void Move(float move, bool isCrouch)
    {
        //���� ���� ��Ҵٸ� ���� �ϴ� ���� ������� �ʱ�ȭ
        if (m_Grounded && m_JumpForce != 17f)
            m_JumpForce -= m_DashToJumpForce;

        //���� ���� ������ �Ұ����� ��ȯ���� ���� ��Ұų� �ɱ�Ű�� ������ �ʾҴٸ� ���������� ����������.
        if ((m_Grounded || !isCrouch) && !AirControl)
            AirControl = true;

        //�����̵� ���°� �ƴϰų� �����ִ� ���¸�
        if (!slide || move == 0)
        {
            m_SlideCool += Time.fixedDeltaTime;         //��Ÿ���� ��� �����Ѵ�.
            m_TimeToSlide = 0f;                         //�����̵� �ϴ� �ð��� �ʱ�ȭ

            //�����̵� ���߿� ����Ű�� ���� ��� -> �����̵� ��� �ɱ� Ű�� ���� ��Ÿ�� �ʱ�ȭ
            if (slide)
            {
                slide = false;
                crouch = true;
                m_SlideCool = 0f;
            }
        }

        //���� �뽬Ű�� ���ȴµ� �÷��̾ �������� �ʴ°��
        if (dash && move == 0)
        {
            dash = false;               //�뽬 ���
            m_TimeToDash = 0f;          //�뽬 �ϴ� �ð��� �ʱ�ȭ
        }

        //�ɱ� ���°� �ƴϰ� �����̵� ���°� �ƴҶ� õ�忡 ���𰡰� �ִٸ� �ɱ� ���·� ����
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
            move *= ImpleCrouch(move, isCrouch);        //�ɱ⸦ �����ϴ� �Լ� ImpleCrouch�� ȣ���� move�� ����

            move *= ImpleSlide(move);                   //�����̵��� �����ϴ� �Լ� ImpleSlide�� ȣ���� move�� ����

            move *= ImpleDash(move, isCrouch);          //�뽬�� �����ϴ� �Լ� ImpleDash�� ȣ���� move�� ����

            //�÷��̾ ������ ������ �ݴ� �������� �����δٸ� ���� �ӵ��� ������
            if ((!m_Grounded && move < 0 && m_JumpFacingRight) || (!m_Grounded && move > 0 && !m_JumpFacingRight))
                move *= m_JumpSpeed;

            //�÷��̾ ������ �ӵ����� targetVelocity�� �ӵ��� �ӵ��� ���� ������
            Vector3 targetVelocity = new Vector2(move * 10f, m_Rigidbody2D.velocity.y);
            m_Rigidbody2D.velocity = Vector3.SmoothDamp(m_Rigidbody2D.velocity, targetVelocity, ref m_Velocity, m_MovementSmoothing);

            //�÷��̾ ��� �ٶ󺸳Ŀ� ���� ������ �ٲٴ� Flip�Լ� ȣ��
            if (move > 0 && !m_FacingRight)
                Flip();
            else if (move < 0 && m_FacingRight)
                Flip();
        }

        ImpleJump(move);        //������ �����ϴ� �Լ� ImpleJump�� ȣ��
    }

    //������ �ٲٴ� Flip�Լ� ����
    private void Flip()
	{
		m_FacingRight = !m_FacingRight;                 //���� m_FacingRight�� �ݴ�� �ٲ۴�

        //�÷��̾��� scale�� -1�� ���� �ݴ� �������� ����
        Vector3 theScale = transform.localScale;
		theScale.x *= -1;
		transform.localScale = theScale;
	}

    //�ɱ⸦ �����ϴ� �Լ� ImpleCrouch�� ����
    private float ImpleCrouch(float move, bool isCrouch)
    {
        //�ɱ� ���°� ���� �ִ� ���
        if (isCrouch && m_Grounded)
        {
            //������ �ɱ� ���°� �ƴϿ��ٸ� �ִϸ��̼��� �۵��ϰ� true���·� �ٲ�
            if (!m_wasCrouching)
            {
                m_wasCrouching = true;
                animator.SetBool("IsCrouching", true);
            }

            //�ɱ� ���°� �Ǿ� ���� �ݶ��̴��� false ���·� ����
            if (m_CrouchDisableCollider != null)
                m_CrouchDisableCollider.enabled = false;

            return m_CrouchSpeed;       //m_CrouchSpeed�� ��ȯ(move�� m_CrouchSpeed�� �ٲ�)
        }
        //���߿��� �ɱ� Ű�� �������(���߿��� ��ũ���� ���)
        else if(isCrouch && !m_Grounded)
        {
            //������ �ɱ� ���¿��ٸ� �ִϸ��̼��� ����ϰ� false���·� �ٲ�
            if (m_wasCrouching)
            {
                m_wasCrouching = false;
                animator.SetBool("IsCrouching", false);
            }

            //�ɱ� ���°� �Ǿ� ���� �ݶ��̴��� false ���·� ����
            if (m_CrouchDisableCollider != null)
                m_CrouchDisableCollider.enabled = false;

            AirControl = false;             //���߿��� �̵� �Ұ�

            return 1f;      //1�� ��ȯ(move�� �״�� ����)
        }
        //�ƴ� ���
        else
        {
            //�ɱ� ���°� �Ǿ� ���� �ݶ��̴� true ���·� ����
            if (m_CrouchDisableCollider != null)
                m_CrouchDisableCollider.enabled = true;

            //������ �ɱ� ���¿��ٸ� �ִϸ��̼��� ����ϰ� false���·� �ٲ�
            if (m_wasCrouching)
            {
                m_wasCrouching = false;
                animator.SetBool("IsCrouching", false);
            }

            return 1f;      //1�� ��ȯ(move�� �״�� ����)
        }
    }

    //�����̵��� �����ϴ� �Լ� ImpleSlide�� ȣ��
    private float ImpleSlide(float move)
    {
        //�����̵� ���°� ���� �ִ� ���
        if (slide && m_Grounded)
        {
            //���� �ִ� �����̵� �ð��� �ʰ��ߴٸ�
            if (m_TimeToSlide > m_maxSlideTime)
            {
                //���� �ݶ��̴��� true ���·� �ٲ�
                if (m_CrouchDisableCollider != null)
                    m_CrouchDisableCollider.enabled = true;

                //�ɱ� ���·� ���ư��� ��Ÿ���� �ʱ�ȭ
                slide = false;
                dash = false;
                crouch = true;
                m_TimeToSlide = 0f;
            }
            //���� ��Ÿ���� �����ٸ�
            else if (m_SlideCool > m_maxSlideCool)
            {
                m_TimeToSlide += Time.fixedDeltaTime;       //�����̵��ϴ� �ð��� ��� ����

                //���� �ݶ��̴��� flase ���·� �ٲ�
                if (m_CrouchDisableCollider != null)
                    m_CrouchDisableCollider.enabled = false;

                return m_SlideSpeed;        //m_SlideSpeed�� ��ȯ(move�� m_SlideSpeed�� �ٲ�)
            }
            //��Ÿ���� ���� ���� �ʾҴٸ� slide�� false ���·� �ٲ�
            else if (m_SlideCool < m_maxSlideCool)
                slide = false;
        }

        return 1f;          //1�� ��ȯ(move�� �״�� ����)
    }

    //�뽬�� �����ϴ� �Լ� ImpleDash�� ȣ��
    private float ImpleDash(float move, bool isCrouch)
    {
        //�뽬�� ������ �����ϸ�(�뽬 ����, ���� ����, �ɱ� ���� �ƴ�, �����̵� ���� �ƴ�)
        if (dash && m_Grounded && !isCrouch && !slide)
        {
            m_TimeToDash += Time.fixedDeltaTime;        //�뽬�ϴ� �ð��� ��� ����

            //���� �뽬�ϴ� �ð��� �뽬 ������ �ʿ��� �ð��� �ѱ�� ������ ��ȭ
            if (m_TimeToDash > m_TimeToDashJump)
                m_JumpForce += m_DashToJumpForce;

            return m_DashSpeed;         //m_DashSpeed�� ��ȯ(move�� m_DashSpeed�� �ٲ�)
        }
        //�뽬�� ���ϴ� ���¶��
        else if (!dash)
        {
            m_TimeToDash = 0f;          //�뽬�ϴ� �ð��� �ʱ�ȭ
            //���� �����ϴ� ���� ���� ���� �ƴ϶�� ���� ��ȭ
            if (m_JumpForce != 17f)
                m_JumpForce -= m_DashToJumpForce;
        }

        return 1f;          //1�� ��ȯ(move�� �״�� ����)
    }

    //������ �����ϴ� �Լ� ImpleJump�� ȣ��
    private void ImpleJump(float move)
    {
        //��������ְų� ������ �ѹ��� ���¿��� ����Ű�� ������
        if ((m_Grounded || m_JumpCount == 1) && jump)
        {
            m_Grounded = false;         //���� �ִ� ���°� �ƴ�
            //���� ������ �ѹ��� ���¶��
            if (m_JumpCount == 2)
            {
                //ĳ������ y�� �ӵ� ����
                m_Rigidbody2D.velocity = new Vector2(m_Rigidbody2D.velocity.x, m_JumpForce);

                //���� �����߳Ŀ� ���� �ô� ���� ����
                if (move > 0)
                    m_JumpFacingRight = true;
                else if (move < 0)
                    m_JumpFacingRight = false;

                m_TimeToDash = 0f;          //�뽬������ ���� ��� �ð� �ʱ�ȭ
            }
            //���� ������ �ι��� ���¶��
            else if (m_JumpCount == 1)
            {
                //�ٶ󺸴� �������� ���ߴ뽬�� ��(AddForce�� ����)
                if (m_FacingRight)
                    m_Rigidbody2D.AddForce(Vector2.right * m_JumpDashForce, ForceMode2D.Impulse);
                else if (!m_FacingRight)
                    m_Rigidbody2D.AddForce(Vector2.left * m_JumpDashForce, ForceMode2D.Impulse);

                //���� ���̻� �ö��� ����
                m_Rigidbody2D.velocity = new Vector2(m_Rigidbody2D.velocity.x, 0);
            }
            m_JumpCount--;          //���� ī��Ʈ�� ����.
        }
    }
}