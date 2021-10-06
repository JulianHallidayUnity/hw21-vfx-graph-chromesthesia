using UnityEngine;

public class VisualInputsManager : MonoBehaviour
{
	[SerializeField]
	private VisualInput[] _visualInputs = {};
	public VisualInput[] VisualInputs => _visualInputs;

	public VisualInput GetVisualInput(int index)
	{
		return _visualInputs.Length > index ? _visualInputs[index] : null;
	}
}