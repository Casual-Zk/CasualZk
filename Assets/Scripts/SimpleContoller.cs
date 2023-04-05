using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Cinemachine;

public class SimpleContoller : MonoBehaviour
{
	[SerializeField] private float m_JumpForce = 400f;                          // Amount of force added when the player jumps.
	[Range(0, 1)][SerializeField] private float m_CrouchSpeed = .36f;           // Amount of maxSpeed applied to crouching movement. 1 = 100%
	[Range(0, .3f)][SerializeField] private float m_MovementSmoothing = .05f;   // How much to smooth out the movement
	[SerializeField] private bool m_AirControl = false;                         // Whether or not a player can steer while jumping;
	[SerializeField] private LayerMask m_WhatIsGround;                          // A mask determining what is ground to the character
	[SerializeField] private Transform m_GroundCheck;                           // A position marking where to check if the player is grounded.
	[SerializeField] private Transform m_CeilingCheck;                          // A position marking where to check for ceilings
	[SerializeField] private Collider2D m_CrouchDisableCollider;                // A collider that will be disabled when crouching

	const float k_GroundedRadius = .2f; // Radius of the overlap circle to determine if grounded
	private bool m_Grounded;            // Whether or not the player is grounded.
	const float k_CeilingRadius = .2f; // Radius of the overlap circle to determine if the player can stand up
	private Rigidbody2D m_Rigidbody2D;
	private bool m_FacingRight = true;  // For determining which way the player is currently facing.
	private Vector3 m_Velocity = Vector3.zero;

	// @Bora variables
	[Header("Casual Edition")]
	[SerializeField] float runSpeed = 40f;
	[SerializeField] GameObject body;
	[SerializeField] Transform lookRight;
	[SerializeField] Transform lookLeft;
	[SerializeField] Transform holdPoint;
	[SerializeField] Transform sprite;
	float horizontalMove = 0f;
	bool jump = false;
	public bool isOwner { get; set; }
	MatchManager matchManager;
	CinemachineVirtualCamera player_v_cam;

	[Header("Weapons")]
	[SerializeField] GameObject[] weapons;
	// 0 - Knife
	// 1 - Glock
	// 2 - Shotgun
	// 3 - M4
	// 4 - AWP
	int activeWeaponIndex = 0;	// Always start with knife

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
	}

    private void Start()
    {
		player_v_cam = FindObjectOfType<CinemachineVirtualCamera>();
	}

    private void Update()
	{
		if (matchManager == null) matchManager = FindObjectOfType<MatchManager>();
		if (matchManager.isGameOver)
        {
			horizontalMove = 0;
			return; // Don't move if the time is up
		}
		
		if (!isOwner) return;

		// @Bora Getting input
		horizontalMove = Input.GetAxisRaw("Horizontal") * runSpeed;

		if (Input.GetButtonDown("Jump")) jump = true;

		LookAtMouse();
		switchWeapon();
	}

    private void FixedUpdate()
	{
		if (!isOwner) return;

		// @Bora Applying movement
		Move(horizontalMove * Time.deltaTime, false, jump);
		jump = false;

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
				if (!wasGrounded)
					OnLandEvent.Invoke();
			}
		}
	}

	public void Move(float move, bool crouch, bool jump)
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
			if (crouch)
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
			}
			else
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

			// Move the character by finding the target velocity
			Vector3 targetVelocity = new Vector2(move * 10f, m_Rigidbody2D.velocity.y);
			// And then smoothing it out and applying it to the character
			m_Rigidbody2D.velocity = Vector3.SmoothDamp(m_Rigidbody2D.velocity, targetVelocity, ref m_Velocity, m_MovementSmoothing);
		}
		// If the player should jump...
		if (m_Grounded && jump)
		{
			// Add a vertical force to the player.
			m_Grounded = false;
			m_Rigidbody2D.AddForce(new Vector2(0f, m_JumpForce));
		}
	}

	private void LookAtMouse()
	{
		Vector3 aimDirection = (GetMousePos() - transform.position).normalized;
		float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
		holdPoint.eulerAngles = new Vector3(0, 0, angle);

		// Reverse the weapon if we look at reverse
		if (angle > 90 || angle < -90)
        {
			body.transform.localScale = new Vector3(-1, 1, 1);
			//body.transform.rotation = Quaternion.Euler(0, 180, 0);
			holdPoint.transform.localScale = new Vector3(-1, -1, 1);
			player_v_cam.Follow = lookLeft;
		}			
		else
        {
			body.transform.localScale = new Vector3(1, 1, 1);
			//body.transform.rotation = Quaternion.identity;
			holdPoint.transform.localScale = new Vector3(1, 1, 1);
			player_v_cam.Follow = lookRight;
		}
	}

	public static Vector3 GetMousePos()
	{
		return Camera.main.ScreenToWorldPoint(Input.mousePosition);
	}

	private void switchWeapon()
    {
		if (Input.GetKeyDown(KeyCode.E))
        {
			// Inactivate the current one
			weapons[activeWeaponIndex].SetActive(false);
			activeWeaponIndex++;

			// Reset value if it exceeds the array
			if (activeWeaponIndex >= weapons.Length) activeWeaponIndex = 0;

			// Activate the new weapon
			weapons[activeWeaponIndex].SetActive(true);
		}
		if (Input.GetKeyDown(KeyCode.Q))
		{
			// Inactivate the current one
			weapons[activeWeaponIndex].SetActive(false);
			activeWeaponIndex--;

			// Get the last weapon if value gets below 0
			if (activeWeaponIndex < 0) activeWeaponIndex = weapons.Length - 1;

			// Activate the new weapon
			weapons[activeWeaponIndex].SetActive(true);
		}
	}
}
