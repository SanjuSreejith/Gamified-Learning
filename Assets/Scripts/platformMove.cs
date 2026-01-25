using System.Collections;
using UnityEngine;

/// <summary>
/// Simple 2D platform vertical motion.
/// Attach to a platform GameObject. The platform will move smoothly up and down around its
/// starting position. Configure `amplitude` (half-distance from center) and `speed` (cycles/sec).
/// </summary>
public class platformMove : MonoBehaviour
{
	[Header("Movement")]
	[Tooltip("Half-distance the platform moves up/down from its start position.")]
	public float amplitude = 1f;
	[Tooltip("Oscillation speed in cycles per second.")]
	public float speed = 1f;
	[Tooltip("Delay (seconds) before starting movement on enable.")]
	public float startDelay = 0f;
	[Tooltip("If true, movement uses localPosition; otherwise world position.")]
	public bool useLocalPosition = true;
	[Tooltip("If true, movement starts automatically on enable.")]
	public bool startOnEnable = true;
	[Tooltip("Phase offset (0..1) to desync multiple platforms. 0 = start phase.")]
	[Range(0f, 1f)]
	public float phaseOffset = 0f;

	Vector3 _startPos;
	float _time;
	bool _moving = true;

	void Awake()
	{
		_startPos = useLocalPosition ? transform.localPosition : transform.position;
		_time = phaseOffset / Mathf.Max(1e-6f, speed);
		_moving = startOnEnable;
	}

	void OnEnable()
	{
		if (startOnEnable && startDelay > 0f)
		{
			_moving = false;
			StartCoroutine(DelayedStart());
		}
		else
		{
			_moving = startOnEnable;
		}
	}

	IEnumerator DelayedStart()
	{
		yield return new WaitForSeconds(startDelay);
		_moving = true;
	}

	void Update()
	{
		if (!_moving) return;

		_time += Time.deltaTime;
		float angle = _time * speed * Mathf.PI * 2f;
		float offsetY = Mathf.Sin(angle) * amplitude; // oscillates between -amplitude .. +amplitude

		Vector3 pos = _startPos + Vector3.up * offsetY;

		if (useLocalPosition)
			transform.localPosition = pos;
		else
			transform.position = pos;
	}

	/// <summary>
	/// Pause movement.
	/// </summary>
	public void Pause() => _moving = false;

	/// <summary>
	/// Resume movement.
	/// </summary>
	public void Resume() => _moving = true;

	/// <summary>
	/// Set the platform's amplitude (half-range).
	/// </summary>
	public void SetAmplitude(float a) => amplitude = a;

	/// <summary>
	/// Set the platform's speed (cycles per second).
	/// </summary>
	public void SetSpeed(float s) => speed = s;

	void OnValidate()
	{
		// keep sensible values in editor
		amplitude = Mathf.Max(0f, amplitude);
		speed = Mathf.Max(0f, speed);
		if (Application.isPlaying == false)
		{
			_startPos = useLocalPosition ? transform.localPosition : transform.position;
		}
	}
}

