using UnityEngine;
using UnityEngine.InputSystem;
public enum BallAxis
{
	XPositive,
	XNegative,
	YPositive,
	YNegative,
	ZPositive,
	ZNegative
}
public class SwipeBall : MonoBehaviour
{
	[SerializeField] private BallAxis m_Face = BallAxis.ZPositive;
	[SerializeField] private float m_GravityMultiplier = 9.81f;
	[SerializeField] private float m_FrictionCoefficient = 0.5f;
	[SerializeField] private bool m_UseKeyboardForTesting = true;
	[SerializeField] private float m_KeyboardTiltSpeed = 2f;
	[SerializeField] private float m_MaxVelocity = 5f;
	public Color ballColor;
	private Rigidbody m_Rigidbody;
	private Vector3 m_SimulatedAccel = Vector3.zero;
	private bool m_IsEnteringHole = false;
	private Vector3 m_TargetHolePosition;
	private float m_HoleEntryTime = 0f;
	private float m_HoleEntryDuration = 0.25f;
	void Start()
	{
		m_Rigidbody = GetComponent<Rigidbody>();
		if (m_Rigidbody == null)
		{
			m_Rigidbody = gameObject.AddComponent<Rigidbody>();
		}
		m_Rigidbody.useGravity = false;
		m_Rigidbody.isKinematic = false;
		m_Rigidbody.freezeRotation = true;

		PhysicsMaterial physicsMaterial = new PhysicsMaterial();
		physicsMaterial.dynamicFriction = 0f;
		physicsMaterial.staticFriction = 0f;
		physicsMaterial.bounciness = 0f;
		physicsMaterial.frictionCombine = PhysicsMaterialCombine.Minimum;
		physicsMaterial.bounceCombine = PhysicsMaterialCombine.Minimum;

		Collider ballCollider = GetComponent<Collider>();
		if (ballCollider != null)
		{
			ballCollider.material = physicsMaterial;
		}

		GameObject maze_walls = GameObject.FindGameObjectWithTag("MazeOutsideWalls");
		if (maze_walls != null)
		{
			Renderer[] renderers = maze_walls.GetComponentsInChildren<Renderer>();
			foreach (Renderer renderer in renderers)
			{
				renderer.enabled = false;
			}
		}

		if (Accelerometer.current != null)
		{
			InputSystem.EnableDevice(Accelerometer.current);
		}
	}
	void Update()
	{
		if (m_UseKeyboardForTesting && Keyboard.current != null)
		{
			Vector2 input = Vector2.zero;
			if (Keyboard.current.wKey.isPressed) input.y = -1f;
			if (Keyboard.current.sKey.isPressed) input.y = 1f;
			if (Keyboard.current.aKey.isPressed) input.x = -1f;
			if (Keyboard.current.dKey.isPressed) input.x = 1f;

			m_SimulatedAccel = Vector3.Lerp(m_SimulatedAccel, new Vector3(input.x, input.y, 0f), Time.deltaTime * m_KeyboardTiltSpeed);
		}
	}
	void FixedUpdate()
	{
		if (GameStateManager.instance != null && !GameStateManager.instance.isPlaying)
		{
			m_Rigidbody.linearVelocity = Vector3.zero;
			return;
		}

		if (m_IsEnteringHole)
		{
			m_HoleEntryTime += Time.fixedDeltaTime;

			float t = Mathf.Clamp01(m_HoleEntryTime / m_HoleEntryDuration);
			float lerpSpeed = Mathf.Lerp(0.1f, 0.5f, t * t);

			transform.position = Vector3.Lerp(transform.position, m_TargetHolePosition, lerpSpeed);

			m_Rigidbody.linearVelocity *= 1.0f - (1.2f * Time.fixedDeltaTime);

			ConstrainVelocity();

			return;
		}

		Vector3 accel;
		if (m_UseKeyboardForTesting)
		{
			accel = m_SimulatedAccel;
		}
		else if (Accelerometer.current != null)
		{
			accel = Accelerometer.current.acceleration.ReadValue();
		}
		else
		{
			return;
		}

		Vector3 gravity = GetGravityDirection(accel) * m_GravityMultiplier;
		m_Rigidbody.AddForce(gravity, ForceMode.Acceleration);

		ConstrainVelocity();
	}
	Vector3 GetGravityDirection(Vector3 accel)
	{
		switch (m_Face)
		{
			case BallAxis.XPositive:
				return new Vector3(0f, -accel.y, accel.x);
			case BallAxis.XNegative:
				return new Vector3(0f, -accel.y, -accel.x);
			case BallAxis.YPositive:
				return new Vector3(-accel.x, 0f, -accel.y);
			case BallAxis.YNegative:
				return new Vector3(-accel.x, 0f, accel.y);
			case BallAxis.ZPositive:
				return new Vector3(-accel.x, -accel.y, 0f);
			case BallAxis.ZNegative:
				return new Vector3(accel.x, -accel.y, 0f);
			default:
				return new Vector3(-accel.x, -accel.y, 0f);
		}
	}
	void ConstrainVelocity()
	{
		if (m_Rigidbody.isKinematic) return;

		Vector3 velocity = m_Rigidbody.linearVelocity;
		switch (m_Face)
		{
			case BallAxis.XPositive:
			case BallAxis.XNegative:
				velocity.x = 0f;
				break;
			case BallAxis.YPositive:
			case BallAxis.YNegative:
				velocity.y = 0f;
				break;
			case BallAxis.ZPositive:
			case BallAxis.ZNegative:
				velocity.z = 0f;
				break;
		}

		if (velocity.magnitude > m_MaxVelocity)
		{
			velocity = velocity.normalized * m_MaxVelocity;
		}

		m_Rigidbody.linearVelocity = velocity;
	}

	public void StartEnteringHole(Vector3 holePosition)
	{
		m_IsEnteringHole = true;
		m_TargetHolePosition = holePosition;
		m_HoleEntryTime = 0f;

		m_Rigidbody.linearVelocity *= 0.3f;

		GameObject[] otherBalls = GameObject.FindGameObjectsWithTag("Ball");
		foreach (GameObject ball in otherBalls)
		{
			if (ball != gameObject)
			{
				Collider ballCollider = ball.GetComponent<Collider>();
				Collider thisCollider = GetComponent<Collider>();
				if (ballCollider != null && thisCollider != null)
				{
					Physics.IgnoreCollision(thisCollider, ballCollider, true);
				}
			}
		}
	}

	public void ResetBall()
	{
		m_IsEnteringHole = false;
		m_HoleEntryTime = 0f;
		ResetPhysics();

		GameObject[] otherBalls = GameObject.FindGameObjectsWithTag("Ball");
		Collider thisCollider = GetComponent<Collider>();
		foreach (GameObject ball in otherBalls)
		{
			if (ball != gameObject && thisCollider != null)
			{
				Collider ballCollider = ball.GetComponent<Collider>();
				if (ballCollider != null)
				{
					Physics.IgnoreCollision(thisCollider, ballCollider, false);
				}
			}
		}
	}

	public void ResetPhysics()
	{
		m_SimulatedAccel = Vector3.zero;

		if (m_Rigidbody != null)
		{
			m_Rigidbody.linearVelocity = Vector3.zero;
			m_Rigidbody.angularVelocity = Vector3.zero;
		}
	}
}
