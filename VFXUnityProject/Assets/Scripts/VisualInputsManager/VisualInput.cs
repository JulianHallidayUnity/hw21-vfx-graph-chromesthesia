using UnityEngine;

[System.Serializable]
public class VisualInput : MonoBehaviour
{
	[Range(0, 1)] private float _inputValue;
	
	public float Value
	{
		get => _inputValue;
		set
		{
			if (AllowUpdates)
			{
				_inputValue = value;
			}
		}
	}

	public bool AllowUpdates = true;
}