using UnityEngine;
using UnityEngine.Events;

public class PressurePlate : MonoBehaviour
{
	[Header("Events")]
	public UnityEvent onActivated = new UnityEvent();
	public UnityEvent onDeactivated = new UnityEvent();

	private bool isActivated = false;
	private int ballsOnPlate = 0;
	private Renderer outerRenderer;
	private Renderer innerRenderer;
	private Color outerBaseColor;
	private Color innerBaseColor;
	private float transitionSpeed = 8.0f;
	private Color outerTargetColor;
	private Color innerTargetColor;

	void Start()
	{
		Transform outerSquare = transform.Find("OuterSquare");
		Transform innerSquare = transform.Find("InnerSquare");

		if (outerSquare != null)
		{
			outerRenderer = outerSquare.GetComponent<Renderer>();
			if (outerRenderer != null)
			{
				outerBaseColor = outerRenderer.material.color;
			}
		}

		if (innerSquare != null)
		{
			innerRenderer = innerSquare.GetComponent<Renderer>();
			if (innerRenderer != null)
			{
				innerBaseColor = innerRenderer.material.color;
			}
		}

		outerTargetColor = outerBaseColor;
		innerTargetColor = innerBaseColor;
	}

	void Update()
	{
		if (outerRenderer != null)
		{
			outerRenderer.material.color = Color.Lerp(outerRenderer.material.color, outerTargetColor, Time.deltaTime * transitionSpeed);
		}
		if (innerRenderer != null)
		{
			innerRenderer.material.color = Color.Lerp(innerRenderer.material.color, innerTargetColor, Time.deltaTime * transitionSpeed);
		}
	}

	void OnTriggerEnter(Collider other)
	{
		if (other.CompareTag("Ball"))
		{
			ballsOnPlate++;
			if (!isActivated)
			{
				Activate();
			}
		}
	}

	void OnTriggerExit(Collider other)
	{
		if (other.CompareTag("Ball"))
		{
			ballsOnPlate--;
			if (ballsOnPlate <= 0 && isActivated)
			{
				ballsOnPlate = 0;
				Deactivate();
			}
		}
	}

	void Activate()
	{
		isActivated = true;
		outerTargetColor = outerBaseColor * 1.5f;
		innerTargetColor = innerBaseColor * 1.5f;
		onActivated.Invoke();
		Debug.Log("Pressure plate activated!");
	}

	void Deactivate()
	{
		isActivated = false;
		outerTargetColor = outerBaseColor;
		innerTargetColor = innerBaseColor;
		onDeactivated.Invoke();
		Debug.Log("Pressure plate deactivated!");
	}

	public bool IsActivated()
	{
		return isActivated;
	}
}
