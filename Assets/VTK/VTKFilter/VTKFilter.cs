using UnityEngine;
using System.Collections;

[System.Serializable]
public abstract class VTKFilter : MonoBehaviour 
{
	[HideInInspector]
	private Kitware.VTK.vtkAlgorithm vtkFilter;
	[HideInInspector]
	public Kitware.VTK.vtkAlgorithm parentVtkFilter;
	[HideInInspector]
	public Kitware.VTK.vtkAlgorithmOutput output;
	[HideInInspector]
	public VTK.FilterType InputType = VTK.FilterType.NotSet;
	[HideInInspector]
	public VTK.FilterType OutputType = VTK.FilterType.NotSet;

	public virtual Kitware.VTK.vtkAlgorithm GetVtkFilter()
	{
		return this.vtkFilter;
	}

	public virtual void SetVtkFilter(Kitware.VTK.vtkAlgorithm vtkFilter)
	{
		this.vtkFilter = vtkFilter;
	}

	//Here set InpuType and OutputType
	protected abstract void OnEnable();

	public virtual Kitware.VTK.vtkAlgorithmOutput UpdateFilter(Kitware.VTK.vtkAlgorithmOutput input)
	{
		if (input == null) 
		{
			input = Kitware.VTK.vtkAlgorithmOutput.New();
		}

		output = GenerateOutput(input);

		return output;
	}

	//Here goes whatever the filter does
	protected abstract Kitware.VTK.vtkAlgorithmOutput GenerateOutput(Kitware.VTK.vtkAlgorithmOutput input);
}
