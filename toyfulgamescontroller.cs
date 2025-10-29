// A WIP attempt at implementing toyful game's character controller. Not going to update since I'm working on a multiplayer game. Feel free to use this as a base for your attempt at a controller.

using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
	[Header("Camera")]
	[SerializeField] private Transform CameraTransform;
	[SerializeField] private float CameraSensitivty = 0.5f;
	[SerializeField] private float TargetCameraHorizontal = 0f, TargetCameraVertical = 0f;
	[SerializeField] private float CameraMaxHorizontal = 15f, CameraMaxVerical = 80f;

	[Header("Physics & Movement")] // Some of these should probably be turned into constants once the values are finalized
	[SerializeField] private bool IsGrounded = false;
	[SerializeField] private float WalkSpeed = 2f, MoveSpeed = 5f, SprintSpeed = 8f;
	private float PlayerSpeed = 1f;
	[SerializeField] private float Acceleration = 200f;
	[SerializeField] private float MaximumAcceleration = 150f;
	[SerializeField] private AnimationCurve AccelerationDotFactor;
    [SerializeField] private AnimationCurve AccelerationDotFactorMax; // not yet used, we'll implement this later
	[SerializeField] protected Vector3 DesiredVelocity	= Vector3.zero;
	[SerializeField] protected Vector3 TargetVelocity	= Vector3.zero;
	[SerializeField] private float JumpForce = 25f;
	[SerializeField] private float StandingHeight = 1f, CroughHeightMultiplier = 0.5f;

	private enum MovementStates {Default, Walking, Crouching, Sprinting};
	[SerializeField] private MovementStates MovementState = 0;

	[SerializeField] private LayerMask MovementCollisionLayers;
	[SerializeField] private Rigidbody MovementRigidBody;
	[SerializeField] private float GroundRaycastLength		= 1.5f, DefaulGroundRaycastLength = 1.5f;
	[SerializeField] private float PhysicsSpringThreshold	= 1.25f, DefaultPhysicsSpringThreshold = 1.25f; // Threshold for spring forces that enact the ride distance
	[SerializeField] private float PhysicsDownForce			= 1f; // unused atm
	[SerializeField] private float PhysicsSpringForce		= 500f;
	[SerializeField] private float PhysicsSpringDamping		= 150f;

	[Header("Input Management")]
	[SerializeField] private InputActionReference IAR_Move;
	[SerializeField] private InputActionReference IAR_Look;
	[SerializeField] private InputActionReference IAR_Jump;
	[SerializeField] private InputActionReference IAR_Sprint;
	[SerializeField] private InputActionReference IAR_Crouch;
	[SerializeField] private InputActionReference IAR_Walk;


	private void Start()
	{
		Cursor.lockState = CursorLockMode.Locked;
		PlayerSpeed = MoveSpeed;
	}

    private void Update()
	{
		ProcessMovementStateMachineInput();
		HandleJumping();
	}

	private void FixedUpdate()
	{
		HandleCameraRotation();
		HandleMovementInput();
		HandleDownForces();
	}

	#region Movement
	/// ┌──────────────────────┐
	/// │ Movement Mathematics │
	/// └──────────────────────┘
	private void HandleMovementInput()
	{
		float delta_time = Time.fixedDeltaTime;

		// Process input
		Vector3 move_input = IAR_Move.action.ReadValue<Vector2>(); move_input = new Vector3(move_input.x, 0f, move_input.y); // move and m_unitgoal
		if (move_input.magnitude > 1f) { move_input.Normalize(); }

		// Transform to camera-relative
		Vector3 camera_forward	= CameraTransform.forward;	camera_forward.y = 0;	camera_forward.Normalize();
		Vector3 camera_right	= CameraTransform.right;	camera_right.y = 0;		camera_right.Normalize();
		DesiredVelocity = (camera_right * move_input.x) + (camera_forward * move_input.z);
		float true_acceleration = Acceleration * AccelerationDotFactor.Evaluate(Vector3.Dot(DesiredVelocity, TargetVelocity.normalized));

		Vector3 platform_velocity = Vector3.zero; // For when standing atop a moving vehicle or platform. Zero for now.
		Vector3 desired_velocity = ((DesiredVelocity * PlayerSpeed) * 1f) + platform_velocity; // The 1f here is meant to represent something, but I forgot what
		TargetVelocity = Vector3.MoveTowards(TargetVelocity, desired_velocity, true_acceleration * delta_time);

		Vector3 target_acceleration = (TargetVelocity - MovementRigidBody.linearVelocity) / delta_time;
		Vector3 valid_acceleration = Vector3.Scale(Vector3.ClampMagnitude(target_acceleration, MaximumAcceleration), new Vector3(1, 0, 1));

		MovementRigidBody.AddForce(valid_acceleration, ForceMode.Acceleration);
		if (valid_acceleration.magnitude > 0.2f) { PerformBodyAlignment(PlayerSpeed * 10); }
	}
	
	private void HandleDownForces()
	{
		Vector3 ray_direction = transform.TransformDirection(Vector3.down);

		RaycastHit ground_cast;
		if (Physics.Raycast(transform.position, ray_direction, out ground_cast, GroundRaycastLength, MovementCollisionLayers)) // todo probably use a shapecast
		{
			#if UNITY_EDITOR
			Debug.DrawRay(transform.position, ray_direction * GroundRaycastLength, Color.yellow);
			Debug.DrawRay(transform.position, ray_direction * ground_cast.distance, Color.red);
			#endif

			Vector3 player_velocity = MovementRigidBody.linearVelocity;
			Vector3 contra_velocity = Vector3.zero;
			Rigidbody collided_with_body = ground_cast.rigidbody;
			if (collided_with_body != null) { contra_velocity = collided_with_body.linearVelocity; }

			float relative_velocity = Vector3.Dot(ray_direction, player_velocity) - Vector3.Dot(ray_direction, contra_velocity);
			float spring_force = ((ground_cast.distance - PhysicsSpringThreshold) * PhysicsSpringForce) - (relative_velocity * PhysicsSpringDamping);

			MovementRigidBody.AddForce(ray_direction * spring_force);
			if (collided_with_body != null) { collided_with_body.AddForceAtPosition(ray_direction * -spring_force, ground_cast.point); }
			IsGrounded = true;
		}
		else
		{
			#if UNITY_EDITOR
			Debug.DrawRay(transform.position, ray_direction * GroundRaycastLength, Color.yellow);
			#endif

			IsGrounded = false;
		}
	}

	/// ┌────────────────────┐
	/// │ Camera Mathematics │
	/// └────────────────────┘
	private void HandleCameraRotation()
	{
		Vector2 mouse_delta = IAR_Look.action.ReadValue<Vector2>();
		TargetCameraHorizontal	+= mouse_delta.x * CameraSensitivty;
        TargetCameraVertical	+= -mouse_delta.y * CameraSensitivty;

		// Move body along the Y axis if our neck reached max rotation
		if (Mathf.Abs(TargetCameraHorizontal) > CameraMaxHorizontal)
		{
			float body_movement_excess = transform.eulerAngles.y + (mouse_delta.x * CameraSensitivty);
			gameObject.transform.localRotation = Quaternion.Euler(transform.eulerAngles.x, body_movement_excess, transform.eulerAngles.z);
		}

		TargetCameraVertical = Mathf.Clamp(TargetCameraVertical, -CameraMaxVerical, CameraMaxVerical);
		TargetCameraHorizontal = Mathf.Clamp(TargetCameraHorizontal, -CameraMaxHorizontal, CameraMaxHorizontal);
		
		CameraTransform.localRotation = Quaternion.Euler(TargetCameraVertical, TargetCameraHorizontal, 0f);
	}

	/// ┌────────────────┐
	/// │ Body Alignment │ For aligning the body with the camera forward
	/// └────────────────┘ when running or aiming
	private void PerformBodyAlignment(float AlignmentRate)
	{
		if (Mathf.Approximately(TargetCameraHorizontal, 0f)) return;
		AlignmentRate *= Time.fixedDeltaTime;
		float step = Mathf.Clamp(TargetCameraHorizontal, -AlignmentRate, AlignmentRate);
		
		// Reduce camera rotation by the same amount that we increase body rotation, zero sum result
		TargetCameraHorizontal -= step;
		MovementRigidBody.MoveRotation(MovementRigidBody.rotation * Quaternion.Euler(0f, step, 0f));
	}

	/// ┌────────────────────────┐
	/// │ Movement State Machine │
	/// └────────────────────────┘
	private void ProcessMovementStateMachineInput()
	{
		if (!IsGrounded) return;
		MovementStates resolved_state = MovementState;

		if		(IAR_Sprint.action.IsPressed())	{ resolved_state = MovementStates.Sprinting; }
		else if	(IAR_Crouch.action.IsPressed()) { resolved_state = MovementStates.Crouching; }
		else if	(IAR_Walk.action.IsPressed())	{ resolved_state = MovementStates.Walking; }
		else 									{ resolved_state = MovementStates.Default; }

		if (resolved_state == MovementState) return;
		MovementState = resolved_state;

		// Set Player Movement Speed
		if (MovementState == MovementStates.Walking || MovementState == MovementStates.Crouching)	{ PlayerSpeed = WalkSpeed; }
		else if (MovementState == MovementStates.Sprinting)											{ PlayerSpeed = SprintSpeed; }
		else																						{ PlayerSpeed = MoveSpeed; }

		// Check Crouch Logic | todo: check ceiling (might need to move logic out of here)
		HandleCrouch(); // By calling it here we ensure the logic only runs once
	}

	/// ┌─────────────────────┐
	/// │ Jumping & Crouching │
	/// └─────────────────────┘
	private void HandleJumping()
	{
		if (IsGrounded && IAR_Jump.action.WasPressedThisFrame() && MovementState != MovementStates.Crouching)
		{
			MovementRigidBody.AddForce(new Vector3(0, JumpForce, 0), ForceMode.Impulse); // currently sticky, find a way to unstick that's reliable and clean
			MovementState = MovementStates.Default;
		}
	}

	private void HandleCrouch()
	{
		if (MovementState == MovementStates.Crouching)
		{
			gameObject.transform.localScale = new Vector3(gameObject.transform.localScale.x, StandingHeight * CroughHeightMultiplier, gameObject.transform.localScale.z);
			GroundRaycastLength = DefaulGroundRaycastLength * CroughHeightMultiplier;
			PhysicsSpringThreshold = DefaultPhysicsSpringThreshold * CroughHeightMultiplier;
			return;
		}
		
		// If not crouching...
		gameObject.transform.localScale = new Vector3(gameObject.transform.localScale.x, StandingHeight, gameObject.transform.localScale.z);
		GroundRaycastLength = DefaulGroundRaycastLength;
		PhysicsSpringThreshold = DefaultPhysicsSpringThreshold;
	}
	#endregion
}
