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
	[SerializeField][Range(0.1f, 0.5f)] float moveSensitivity = 0.2f;
	//[SerializeField] [Range(0.6f, 0.9f)] float jumpSensitivity = 0.2f;  // OLD SYSTEM
	//[SerializeField] [Range(0.3f, 1f)] float fireSensitivity = 0.8f;    // OLD SYSTEM
	//[SerializeField] Joystick fireJoystick;	// OLD SYSTEM
	[SerializeField] Joystick moveJoystick;
	float horizontalMove = 0f;
	bool jump = false;
	bool crouch = false;
	public bool isOwner { get; set; }
	CinemachineVirtualCamera player_v_cam;

	[Header("Weapons")]
	[SerializeField] GameObject[] weapons;
	[SerializeField] Image weaponUI;
	[SerializeField] Sprite[] weaponSprites;
	[SerializeField] Slider reloadingSlider;
	[SerializeField] Image reloadingImage;
	// 0 - Knife
	// 1 - Glock
	// 2 - Shotgun
	// 3 - M4
	// 4 - AWP
	int[] magSizes = new int[5];
	int[] magCounts = new int[5];
	float reloadingCounter = -1;
	int activeWeaponIndex = 0;  // Always start with knife
	[SerializeField] LayerMask playerLayer;
	[SerializeField] Transform knifeHitCenter;	// Knife attack
	[SerializeField] [Range(0.1f, 5f)] float knifeHitRange = 0.3f;
	[SerializeField] GameObject controlUI;
	[SerializeField] GameObject hitRangeUI;
	[SerializeField] Image healthFillImage;
	[SerializeField] Image onTargetImage;
	[SerializeField] TextMeshProUGUI ammoCounterText;
	[SerializeField] Image autoBackground;	// Auto Aim
	[SerializeField] Image autoText;        // Auto Aim
	[SerializeField] public bool isThisFake;        // Test purposes
	[SerializeField] float weaponRotationSpeed;        // Auto Aim
	[SerializeField] Collider2D targetCollider;	// Auto aim
	Collider2D[] hitEnemies;	// Knife attacks
	Animator playerAnimator;

	// Save colors to enable/disable auto button
	Color autoBackgrounEnabledColor;
	Color autoBackgrounDisabledColor;
	Color autoTextEnabledColor;
	Color autoTextDisabledColor;
	bool fireButtonDown = false;	// Fire button down event
	Transform targetPos;	// closes target
	List<SimpleContoller> enemyList = new List<SimpleContoller>();  // In range

	MatchManager matchManager;
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
		if (isThisFake) return;
		m_Rigidbody2D = GetComponent<Rigidbody2D>();

		if (OnLandEvent == null)
			OnLandEvent = new UnityEvent();

		if (OnCrouchEvent == null)
			OnCrouchEvent = new BoolEvent();
	}

    private void Start()
    {
		if (isThisFake) return;
		player_v_cam = FindObjectOfType<CinemachineVirtualCamera>();
		audioManager = FindObjectOfType<AudioManager>();
		dm = FindObjectOfType<FirebaseDataManager>();
		playerAnimator = GetComponent<Animator>();

		if (isOwner)
        {
			player_v_cam.Follow = followCamPosition;    // Set cam to follow
			healthFillImage.color = Color.cyan;			// Set health color to cyan to distinguish 

			// Save enabled and disabled colors of auto button (images)
			Color bColor = autoBackground.color;
			autoBackgrounEnabledColor = bColor;
			autoBackgrounDisabledColor = new Color(bColor.r, bColor.g, bColor.b, 0.6f);

			Color tColor = autoText.color;
			autoTextEnabledColor = tColor;
			autoTextDisabledColor = new Color(tColor.r, tColor.g, tColor.b, 0.6f);

			// Set dynamic player variables
			player_v_cam.m_Lens.OrthographicSize = dm.dv.player_CamOrthoSize;
			player_v_cam.GetCinemachineComponent<CinemachineFramingTransposer>().m_XDamping = dm.dv.player_CamXdamping;
			camDistance = dm.dv.player_FollowCamDistance;
			GetComponent<Health>().SetHealth(dm.dv.player_Health);
			m_JumpForce = dm.dv.player_JumpForce;
			runSpeed = dm.dv.player_RunSpeed;

			// Get mag sizes
			magSizes[1] = dm.magSize[1];
			magSizes[3] = dm.magSize[3];

			// Load mags with ammo
			if (dm.ammoBalance[1] > 0)
            {
				// Check if we have enough to fill
				if (dm.ammoBalance[1] >= magSizes[1])
					magCounts[1] = magSizes[1];
				else
					magCounts[1] = dm.ammoBalance[1];
			}
			if (dm.ammoBalance[3] > 0)
			{
				// Check if we have enough to fill
				if (dm.ammoBalance[3] >= magSizes[3])
					magCounts[3] = magSizes[3];
				else
					magCounts[3] = dm.ammoBalance[3];
			}

			reloadingImage.enabled = false;	// Starting with knife, therefore close the reloading image
		}
		else
        {
			controlUI.SetActive(false);  // Close other's control UI
			hitRangeUI.SetActive(false); // Close other's range UI
		}

		// Display Username
		if (!GetComponent<PhotonView>().IsMine)
			nameText.text = GetComponent<PhotonView>().Controller.NickName;
		else
			nameText.text = PlayerPrefs.GetString("Nickname");
	}

    private void Update()
	{
		if (isThisFake) return;
		if (matchManager == null) matchManager = FindObjectOfType<MatchManager>();

		// Check if game is over
		if (matchManager.isGameOver)
        {
			horizontalMove = 0;
			controlUI.SetActive(false);
			return; // Don't move if the time is up
		}
		else if (isOwner) { controlUI.SetActive(true); }

		// If reloading, move slider
		if (reloadingCounter > 0)
        {
			reloadingCounter = reloadingCounter - Time.deltaTime;
			reloadingSlider.value = reloadingCounter;
        }

		// DEBUG fire with keyboard
		//if (Input.GetKeyDown(KeyCode.A)) Btn_Fire(true);
		//else Btn_Fire(false);

		// vel return +/-13
		if (Mathf.Abs(m_Rigidbody2D.velocity.x) > 2f) 
			playerAnimator.SetBool("Walking", true);
		else 
			playerAnimator.SetBool("Walking", false);

		if (!isOwner) return;	// Below code is valid for just the owner

		// @Bora Getting input
		// Player Movement
		if (Mathf.Abs(moveJoystick.Horizontal) > moveSensitivity)
		{
			horizontalMove = moveJoystick.Horizontal * runSpeed;

			if (moveJoystick.Horizontal > 0)
				followCamPosition.localPosition = new Vector3(camDistance, 0f, 0f);
			else
				followCamPosition.localPosition = new Vector3(-camDistance, 0f, 0f);
		}
		else horizontalMove = 0;

		// Jump or Crouch
		//if (moveJoystick.Vertical > jumpSensitivity) jump = true; else jump = false;
		//if (moveJoystick.Vertical < -jumpSensitivity) crouch = true; else crouch = false;

		Aim();
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

	// Detect enemies with target detection colliders (Trigger)
    private void OnTriggerEnter2D(Collider2D collider)
    {
		if (!isOwner) return;

		if (collider.GetComponent<SimpleContoller>()) 
		{
			// Ignore enemy's target collider
			if (collider.isTrigger) return;

			// Get enemy controller
			SimpleContoller enemy = collider.GetComponent<SimpleContoller>();

			// If target is not on the target list, then add it.
			if (!enemyList.Contains(enemy)) enemyList.Add(enemy);

			Debug.Log("Target in range!: " + collider.name); 
		}
    }

    private void OnTriggerExit2D(Collider2D collider)
    {
		if (!isOwner) return;

		if (collider.GetComponent<SimpleContoller>())
		{
			// Ignore enemy's target collider
			if (collider.isTrigger) return;

			SimpleContoller enemy = collider.GetComponent<SimpleContoller>();

			// If target is in the target list, then remove it.
			if (enemyList.Contains(enemy)) enemyList.Remove(enemy);

			// Turn off the target UI if the enemy is out of range
			enemy.OnTarget(false);

			Debug.Log("Target out of the range!: " + collider.name);
		}
	}

    private void Aim()
	{
		targetPos = followCamPosition;	// by default

		// if there are multiple targets in the list, find the closest one
		if (enemyList.Count >= 2) 
		{
			float currentDistance = 10000000;	// set a too high value to compare with the first enemy
			float targetDistance;			// to save target distance
			float checkDistance;            // to save target distance

			// Iterate through all enemies
			foreach (SimpleContoller enemy in enemyList)
            {
				// Save distance and compare
				targetDistance = Vector3.Distance(enemy.transform.position, this.transform.position);

				if (targetDistance < currentDistance)
                {
					// If it is closer then current one, then save this as the target
					currentDistance = targetDistance;
					targetPos = enemy.transform;
					enemy.OnTarget(true);
				}
            }

			// Iterate again to close target UI on the ones who are not target
			foreach (SimpleContoller enemy in enemyList)
			{
				// Get the target's distance
				checkDistance = Vector3.Distance(enemy.transform.position, this.transform.position);

				// If it is not current target's distance, then close its target UI
				if (!Mathf.Approximately(checkDistance, currentDistance)) enemy.OnTarget(false);
			}
		}
		// If there is just 1 enemy in the list, then just target it
		else if (enemyList.Count == 1)
        {
			targetPos = enemyList[0].transform;
			enemyList[0].OnTarget(true);	// Open target UI on enemy
		}
		// If no target, just look strait left and right based on movement
		else if (Mathf.Abs(moveJoystick.Horizontal) > moveSensitivity)
		{
			if (moveJoystick.Horizontal > 0)
				followCamPosition.localPosition = new Vector3(camDistance, 0f, 0f);
			else
				followCamPosition.localPosition = new Vector3(-camDistance, 0f, 0f);
		}

		// JUST FOR CAMERA: if we have target, move camera to the location of the target's side. No up/down
		if (enemyList.Count >= 1)
		{
			if (targetPos.position.x > transform.position.x)
				followCamPosition.localPosition = new Vector3(camDistance, 0f, 0f);
			else
				followCamPosition.localPosition = new Vector3(-camDistance, 0f, 0f);
		}

		// Turn body and holding point based on target location
		if (targetPos.position.x > transform.position.x)
		{
			body.transform.localScale = new Vector3(1, 1, 1);
			holdPoint.transform.localScale = new Vector3(1, 1, 1);
		}
		else
        {
			body.transform.localScale = new Vector3(-1, 1, 1);
			holdPoint.transform.localScale = new Vector3(-1, -1, 1);
		}

		// Rotate weapon
		Vector3 vectorToTarget = targetPos.position - holdPoint.position;  // Get vector
		float angle = Mathf.Atan2(vectorToTarget.y, vectorToTarget.x) * Mathf.Rad2Deg;  // Calculate angle
		Quaternion q = Quaternion.AngleAxis(angle, Vector3.forward);    // Create quaternion
		holdPoint.rotation = q; // Apply to holding point

		// Add turn speed if rotation speed is set
		if (weaponRotationSpeed > 0)
			holdPoint.rotation = Quaternion.Slerp(holdPoint.rotation, q, Time.deltaTime * weaponRotationSpeed);

		////////////    Manuel Aim System    ////////////
		/*
		targetLocation = followCamPosition.localPosition;
		float angle = Mathf.Atan2(target.position.y, target.position.x) * Mathf.Rad2Deg;

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
		*/
	}

	// Enable/disable target UI on this player if gets targetted
	public void OnTarget(bool isThisTarget)
    {
		if (isThisTarget) onTargetImage.enabled = true;
		else onTargetImage.enabled = false;
    }

	public void Btn_SwitchWeapon()
	{
		photonView.RPC("SwitchWeapon", RpcTarget.All, true);
	}
	public void Btn_Jump() { jump = true; }
	private void StopCurrentWeaponSound()
	{
		if (activeWeaponIndex == 2) photonView.RPC("StopFireSFX", RpcTarget.All, "Shotgun_SFX");
		if (activeWeaponIndex == 4) photonView.RPC("StopFireSFX", RpcTarget.All, "Sniper_SFX");
	}

	public void Btn_Fire(bool fire) { fireButtonDown = fire; }

	// Turns bool to weapon if player able to fire
	public bool CanFire()
    {
		if (!fireButtonDown) return false;  // Can't fire if the button is not down
		if (activeWeaponIndex == 0) return true;    // if we hold knife, then always can fire

		// Fire if the mag has ammo
		if (activeWeaponIndex == 1 || activeWeaponIndex == 3)
			return magCounts[activeWeaponIndex] > 0;
		else 
			return dm.ammoBalance[activeWeaponIndex] > 0;

		////////////    Manuel Aim System    ////////////
		/*	
		if (Mathf.Abs(fireJoystick.Horizontal) > fireSensitivity || Mathf.Abs(fireJoystick.Vertical) > fireSensitivity)
		{
			if (activeWeaponIndex == 0) return true;
			return dm.ammoBalance[activeWeaponIndex] > 0;
		}
		else return false;
		*/
	}

	private void ReloadMag(int index)
    {
		// Display current situation first
		ammoCounterText.text = magCounts[index].ToString() + "/" +
			(dm.ammoBalance[index] - magCounts[index]).ToString();

		// Reflext the mag size into reloading UI
		reloadingSlider.value--;

		// Reload the mag if its empty and we have ammo to load
		if (magCounts[index] <= 0 && dm.ammoBalance[index] > 0)
		{
			StartCoroutine(Reloading(index));
		}		
	}

	private IEnumerator Reloading(int index)
	{
		reloadingImage.color = Color.red;

		if (index == 1)
        {
			audioManager.Play("ReloadSFX_Glock");

			reloadingSlider.maxValue = reloadingCounter = 3.2f;

			yield return new WaitForSeconds(3.2f);
		}
		else if (index == 3)
        {
			audioManager.Play("ReloadSFX_M4");

			reloadingSlider.maxValue = reloadingCounter = 4.8f;
			
			yield return new WaitForSeconds(4.8f);
		}

		Debug.Log("Max Value: " + reloadingSlider.maxValue);
		Debug.Log("Value: " + reloadingSlider.value);

		reloadingImage.color = Color.green;

		// If we have enough to fill fully, then fill it with size
		if (dm.ammoBalance[index] >= magSizes[index])
			magCounts[index] = magSizes[index];
		// Else, fill it with what we have
		else
			magCounts[index] = dm.ammoBalance[index];

		ammoCounterText.text = magCounts[index].ToString() + "/" +
			(dm.ammoBalance[index] - magCounts[index]).ToString();

		if (activeWeaponIndex == 1 || activeWeaponIndex == 3)
		{
			reloadingSlider.maxValue = magSizes[activeWeaponIndex];
			reloadingSlider.value = magCounts[activeWeaponIndex];
		}
	}

	// Updates ammo on local
	public void Fired() 
	{
		if (activeWeaponIndex == 0) return;
		dm.ammoBalance[activeWeaponIndex]--;

		// If we have a weapon has extenable mag feature
		if (activeWeaponIndex == 1 || activeWeaponIndex == 3)
        {
			magCounts[activeWeaponIndex]--;
			ReloadMag(activeWeaponIndex);
		}

		// If not, just update the UI as it was
		else
        {
			ammoCounterText.text = dm.ammoBalance[activeWeaponIndex].ToString();
			reloadingSlider.maxValue = reloadingSlider.value = 1;
		}
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
			while (dm.weaponBalance[activeWeaponIndex] <= 0);
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
			while (dm.weaponBalance[activeWeaponIndex] <= 0);
		}

		// Activate the new weapon
		weapons[activeWeaponIndex].SetActive(true);
		weaponUI.sprite = weaponSprites[activeWeaponIndex];

		if (activeWeaponIndex == 1 || activeWeaponIndex == 3)
		{
			ammoCounterText.text = magCounts[activeWeaponIndex].ToString() + "/" +
				(dm.ammoBalance[activeWeaponIndex] - magCounts[activeWeaponIndex]).ToString();

			reloadingSlider.maxValue = magSizes[activeWeaponIndex];
			reloadingSlider.value = magCounts[activeWeaponIndex];
		}
		else
			ammoCounterText.text = dm.ammoBalance[activeWeaponIndex].ToString();

		// Update magsize UI
		if (activeWeaponIndex == 1)
        {
			reloadingSlider.maxValue = magSizes[1];
			reloadingSlider.value = magCounts[1];
		}
		else if (activeWeaponIndex == 3)
        {
			reloadingSlider.maxValue = magSizes[3];
			reloadingSlider.value = magCounts[3];
		}

		if (activeWeaponIndex == 0) ammoCounterText.text = "";  // clean knife counter

		// Close reloading image if we hold a knife
		if (activeWeaponIndex == 0) reloadingImage.enabled = false;
		else reloadingImage.enabled = true;
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
		foreach (Collider2D enemyCollider in hitEnemies) {
			if(enemyCollider.isTrigger) continue; // don't include target detection colliders

			int enemyID = enemyCollider.GetComponent<PhotonView>().GetInstanceID();

			if (enemyID == GetComponent<PhotonView>().GetInstanceID()) continue; // don't hurt itself
			if (ids.Contains(enemyID)) continue;	// Don't give double damage to enemy
			ids.Add(enemyID);

			if (!isOwner) return;

			enemyCollider.GetComponent<PhotonView>().RPC("TakeDamage", RpcTarget.All, 
				damage, ownerName, enemyCollider.GetComponent<PhotonView>().Controller.NickName);

			photonView.RPC("PlayFireSFX", RpcTarget.All, "Knife_Damage_SFX");
		}
	}
	private void OnDrawGizmos()
	{
		if (isThisFake) return;
		Gizmos.DrawWireSphere(knifeHitCenter.position, knifeHitRange);

		if (!targetPos) return;
		Vector2 enemyPos = new Vector2(targetPos.position.x, targetPos.position.y);
		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere(enemyPos, knifeHitRange + 1);
	}

	[PunRPC]
	public void PlayFireSFX(string soundName) { audioManager.Play(soundName); }
	
	[PunRPC]
	public void StopFireSFX(string soundName) { audioManager.Stop(soundName); }
}
