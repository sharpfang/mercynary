﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerController : MonoBehaviour {

	public float Speed = 5;
	public float DashDistance = 1f;
	public float MaxDashingSpeed = 3f;
	public float DashCooldownTime = 5f;
	public AnimationCurve DashCurve;
	public Text Text;
	public GameObject halo;

	Rigidbody2D rb;
	bool dashing;
	bool dashOnCooldown;
	Vector2 dashingTarget;
	float dashTimer = 0; 
	int layerMask;
	int playerMaxHealth = 100;
	int playerCurrentHealth = 100;
	float resurrectRingRange = 0.1f;
	SpriteRenderer spriteRenderer;
	Animator animator;
	AudioClip[] dashes;
	AudioClip[] resurrectionSounds;
	AudioSource source;

	// Use this for initialization
	void Start () {
		rb = GetComponent<Rigidbody2D>();
		dashing = false;
		dashOnCooldown = false;
		layerMask |= 1 << LayerMask.NameToLayer ("Resurrectable");
		spriteRenderer = GetComponentInChildren<SpriteRenderer> ();
		animator = GetComponentInChildren<Animator> ();
		dashes = Resources.LoadAll<AudioClip>("Sounds/Dash");
		resurrectionSounds = Resources.LoadAll<AudioClip>("Sounds/Resurrect");
		source = GetComponent<AudioSource> ();
	}
	
	// Update is called once per frame
	void Update () {
		if (Input.GetAxis ("Horizontal") > 0) {
			GetComponentInChildren<SpriteRenderer> ().flipX = true;
		} else {
			GetComponentInChildren<SpriteRenderer> ().flipX = false;
		}

		if (!dashing) {
			rb.MovePosition (new Vector2 (
				transform.position.x + Input.GetAxis ("Horizontal") * Time.deltaTime * Speed,
				transform.position.y + Input.GetAxis ("Vertical") * Time.deltaTime * Speed
			));

			if (!dashOnCooldown && Input.GetButtonDown ("Dash")) {
				dashing = true;
				source.PlayOneShot (dashes [Random.Range (0, dashes.Length)]);
				animator.SetBool ("Dashing", dashing);
				dashOnCooldown = true;
				Vector3 dashingDirection = new Vector3 (
					Input.GetAxis ("Horizontal"),
					Input.GetAxis ("Vertical"),
					0
				).normalized;
				dashingTarget = transform.position + dashingDirection * DashDistance;
				StartCoroutine (DashCooldown ());
			}
		} else {
			dashTimer += Time.deltaTime;
			float curveValue = DashCurve.Evaluate (dashTimer);
			rb.MovePosition (Vector2.MoveTowards (transform.position, dashingTarget, curveValue * MaxDashingSpeed));
			spriteRenderer.color = new Color (1 - curveValue, 1 - curveValue, 1 - curveValue);
			halo.GetComponent<SpriteRenderer>().color = new Color (1, 1, 1, 1 - curveValue);
			if (Vector2.Distance (transform.position, dashingTarget) < 0.1) {
				dashing = false;
				animator.SetBool ("Dashing", dashing);
				spriteRenderer.color = new Color (1, 1, 1);
				halo.GetComponent<SpriteRenderer>().color = new Color (1, 1, 1, 1);
			}
		}
		if (Input.GetButtonDown("Resurrect")) {
			Collider2D[] hits = Physics2D.OverlapCircleAll (transform.position, resurrectRingRange, layerMask); // We kind of need the colliders on
			animator.SetTrigger("Resurrection");
			StartCoroutine (Resurrection (hits));
			resurrectRingRange = 0.1f;
		}
		resurrectRingRange += Time.deltaTime / 2;
		if (resurrectRingRange > 2f) {
			resurrectRingRange = 2f;
		}
		halo.transform.localScale = new Vector3 (resurrectRingRange * 2 * Mathf.PI, resurrectRingRange * 2 * Mathf.PI, 1);
	}

	IEnumerator Resurrection(Collider2D[] hits){
		foreach (Collider2D hit in hits) {
			Unit hitUnit = hit.gameObject.GetComponent<Unit> ();
			if (hitUnit != null && hitUnit.Owner == Owner.Ally) {
				hitUnit.Resurrect ();
				source.PlayOneShot (resurrectionSounds [Random.Range (0, dashes.Length)]);
				yield return new WaitForSeconds (Random.Range (0.05f, 0.15f));
			}
		}
	}

	IEnumerator DashCooldown(){
		yield return new WaitForSeconds (DashCooldownTime);
		dashOnCooldown = false;
		dashTimer = 0;
	}

	void OnTriggerEnter2D(Collider2D col){
		if (col.tag == "Unit") {
			if (col.GetComponent<Unit> ().Owner == Owner.Enemy && !dashing) {
				TakeDamage ();
			}
		}
	}

	public void TakeDamage(){
        if (dashing) return;
		playerCurrentHealth--;
		if (playerCurrentHealth < 0) {
			Text.text = "Health: " + 0;
		} else {
			Text.text = "Health: " + playerCurrentHealth;
		}
		if (playerCurrentHealth < 0) {
			FindObjectOfType<GameLogic> ().GameOver ();
		}
	}
}
