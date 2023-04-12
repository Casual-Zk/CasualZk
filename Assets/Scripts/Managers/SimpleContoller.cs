using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using Cinemachine;
using Photon.Pun;
using TMPro;

public class SimpleContoller : MonoBehaviourPunCallbacks
{
	[SerializeField] private float m_JumpForce = 750f;                          // Amount of force added when the player jumps.
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
	[SerializeField] TextMeshProUGUI nameText;
	[SerializeField] PlayerInfo playerInfo;
	[SerializeField] float runSpeed = 40f;
	[SerializeField] GameObject body;
	[SerializeField] Transform holdPoint;

    [Header("Move")]
	[SerializeField] Transform followCamPosition;
	[SerializeField] float camDistance = 5f;
	[SerializeField] float camVerticalDrawBack = 2f;
	[SerializeField] [Range(0.1f, 0.5f)] float moveSensitivity = 0.2f;
	[SerializeField] [Range(0.6f, 0.9f)] float jumpSensitivity = 0.2f;
	[SerializeField] [Range(0.8f, 1f)] float fireSensitivity = 0.8f;
	[SerializeField] Joystick moveJoystick;
	[SerializeField] Joystick fireJoystick;
	float horizontalMove = 0f;
	bool jump = false;
	bool crouch = false;
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
	int activeWeaponIndex = 0;  // Always start with knife
	[SerializeField] LayerMask playerLayer;
	[SerializeField] Transform knifeHitCenter;
	[SerializeField] [Range(0.1f, 5f)] float knifeHitRange = 0.3f;
	[SerializeField] GameObject playerUI;
	[SerializeField] TextMeshProUGUI ammoCounterText;
	Collider2D[] hitEnemies;
	Animator playerAnimator;


	AudioManager audioManager;
	FirebaseDataManager dm;

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
		audioManager = FindObjectOfType<AudioManager>();
		dm = FindObjectOfType<FirebaseDataManager>();
		playerAnimator = GetComponent<Animator>();

		player_v_cam.Follow = followCamPosition;

		// Display Username
		if (!GetComponent<PhotonView>().IsMine)
			nameText.text = GetComponent<PhotonView>().Controller.NickName;
		else
			nameText.text = PlayerPrefs.GetString("Nickname");


		if (!isOwner) playerUI.SetActive(false);
	}

    private void Update()
	{
		if (matchManager == null) matchManager = FindObjectOfType<MatchManager>();
		if (matchManager.isGameOver)
        {
			horizontalMove = 0;
			playerUI.SetActive(false);
			return; // Don't move if the time is up
		}

		if (!isOwner) return;

		// @Bora Getting input
		// Player Movement
		if (Mathf.Abs(moveJoystick.Horizontal) > moveSensitivity)
		{
			horizontalMove = moveJoystick.Horizontal * runSpeed;
		}
		else horizontalMove = 0;

		// Camera Movement
		followCamPosition.localPosition = new Vector3
			(camDistance * fireJoystick.Horizontal, 
			(camDistance - camVerticalDrawBack) * fireJoystick.Vertical, 0f);

		// Jump or Crouch
		if (moveJoystick.Vertical > jumpSensitivity && m_Grounded) jump = true; else jump = false;
		//if (moveJoystick.Vertical < -jumpSensitivity) crouch = true; else crouch = false;

		LookAround();
	}

	private void FixedUpdate()
	{
		if (!isOwner) return;

		// @Bora Applying movement
		Move(horizontalMove * Time.deltaTime, crouch, jump);
		jump = false;
		crouch = false;

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

	private void LookAround()
	{
		Vector3 aimDirection = followCamPosition.localPosition;
		float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;

		// Don't rotate if we hold knife
		if (activeWeaponIndex == 0) holdPoint.eulerAngles = new Vector3(0, 0, 0);
		else holdPoint.eulerAngles = new Vector3(0, 0, angle);


		// Reverse the weapon if we look at reverse
		if (angle > 90 || angle < -90)
        {
			body.transform.localScale = new Vector3(-1, 1, 1);
			holdPoint.transform.localScale = new Vector3(-1, -1, 1);
		}			
		else
        {
			body.transform.localScale = new Vector3(1, 1, 1);
			holdPoint.transform.localScale = new Vector3(1, 1, 1);
		}

		// If we hold knife, correct the holding point
		if (activeWeaponIndex == 0)
			holdPoint.transform.localScale = new Vector3(1, 1, 1);
	}

	public void Btn_SwitchWeapon()
	{
		photonView.RPC("SwitchWeapon", RpcTarget.All, true);
	}
	private void StopCurrentWeaponSound()
	{
		if (activeWeaponIndex == 2) photonView.RPC("StopFireSFX", RpcTarget.All, "Shotgun_SFX");
		if (activeWeaponIndex == 4) photonView.RPC("StopFireSFX", RpcTarget.All, "Sniper_SFX");
	}

	public bool Fire()
    {
		if (Mathf.Abs(fireJoystick.Horizontal) > fireSensitivity || Mathf.Abs(fireJoystick.Vertical) > fireSensitivity)
		{
			if (activeWeaponIndex == 0) return true;
			return dm.ammoBalance[activeWeaponIndex] > 0;
		}
		else return false;

    }

	public void Fired() 
	{
		if (activeWeaponIndex == 0) return;
		dm.ammoBalance[activeWeaponIndex]--;
		ammoCounterText.text = dm.ammoBalance[activeWeaponIndex].ToString();
	}

	[PunRPC]
	public void SwitchWeapon(bool switchRight)
    {
		if (switchRight)
		{
			//StopCurrentWeaponSound();

			// Inactivate the current one
			weapons[activeWeaponIndex].SetActive(false);

			// Increase the index until find a owned weapon
			do 
			{ 
				activeWeaponIndex++;

				// If we go beyond the range, reset the index
				if (activeWeaponIndex >= weapons.Length) activeWeaponIndex = 0; 
			}
			while (!dm.hasWeapon[activeWeaponIndex]);

			// Activate the new weapon
			weapons[activeWeaponIndex].SetActive(true);
			ammoCounterText.text = dm.ammoBalance[activeWeaponIndex].ToString();
		}
		else
		{
			//StopCurrentWeaponSound();

			// Inactivate the current one
			weapons[activeWeaponIndex].SetActive(false);

			// Decrease the index until find a owned weapon
			do
			{
				activeWeaponIndex--;

				// If we go beyond the range, reset the index
				if (activeWeaponIndex < 0) activeWeaponIndex = weapons.Length - 1;
			}
			while (!dm.hasWeapon[activeWeaponIndex]);

			// Activate the new weapon
			weapons[activeWeaponIndex].SetActive(true);
			ammoCounterText.text = dm.ammoBalance[activeWeaponIndex].ToString();
		}

		if (activeWeaponIndex == 0) ammoCounterText.text = "";  // clean knife counter
	}

	[PunRPC]
	public void TriggerKnife(int damage, string ownerName) 
	{
		playerAnimator.SetTrigger("Knife_Attack");

		// Gets all the player colliders including player's two own collider itself
		hitEnemies = Physics2D.OverlapCircleAll(knifeHitCenter.position, knifeHitRange, playerLayer);

		// Create a id list to prevent double execution due to double collider
		List<int> ids = new List<int>();

		// Give damage
		foreach (Collider2D enemy in hitEnemies) {
			int enemyID = enemy.GetComponent<PhotonView>().GetInstanceID();

			if (ids.Contains(enemyID)) continue;
			ids.Add(enemyID);

			if (!isOwner) return;

			enemy.GetComponent<PhotonView>().RPC("TakeDamage", RpcTarget.All, 
				damage, ownerName, enemy.GetComponent<PhotonView>().Controller.NickName);

			photonView.RPC("PlayFireSFX", RpcTarget.All, "Knife_Damage_SFX");
		}
	}
	private void OnDrawGizmos()
	{
		Vector2 drawGismos = new Vector2(knifeHitCenter.position.x, knifeHitCenter.position.y);
		Gizmos.DrawWireSphere(drawGismos, knifeHitRange);

	}

	[PunRPC]
	public void PlayFireSFX(string soundName) { audioManager.Play(soundName); }
	
	[PunRPC]
	public void StopFireSFX(string soundName) { audioManager.Stop(soundName); }
}
