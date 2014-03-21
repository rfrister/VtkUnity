using UnityEngine;
using System.Collections.Generic;
using Kitware.VTK;

[ExecuteInEditMode]
public class VTKProperties : MonoBehaviour 
{
	[HideInInspector]
	public string[] dataArray;
	[HideInInspector]
	public int selectedDataArray;
	[HideInInspector]
	public string[] Lut = {"BLUE_RED", "RED_BLUE", "RAINBOW"};
	[HideInInspector]
	public int selectedLut;
	[HideInInspector]
	public string[] typesOfColor = {"solid color", "data"};
	[HideInInspector]
	public int selectedColorType;

	public void Read()
	{
		VTKRoot root = gameObject.GetComponent<VTKRoot> ();

		VTK.ReaderType readerType = root.readerType;

		vtkXMLPolyDataReader polyDataReader;

		vtkXMLUnstructuredGridReader unstructuredGridReader;

		if( readerType == VTK.ReaderType.PolyData )
		{
			polyDataReader = root.polyDataReader;

			dataArray = new string[polyDataReader.GetNumberOfPointArrays() + polyDataReader.GetNumberOfCellArrays()];

			//Cell data
			for (int i = 0; i < polyDataReader.GetNumberOfCellArrays(); i++) 
			{
				dataArray[i] = polyDataReader.GetCellArrayName(i) + " [C]";
			}

			//Point data
			for (int i = 0; i < polyDataReader.GetNumberOfPointArrays(); i++) 
			{
				dataArray[polyDataReader.GetNumberOfCellArrays() + i] = polyDataReader.GetPointArrayName(i) + " [P]";
			}
		}

		if(readerType == VTK.ReaderType.UnstructuredGrid)
		{
			unstructuredGridReader = root.unstructuredGridReader;

			dataArray = new string[unstructuredGridReader.GetNumberOfPointArrays() + unstructuredGridReader.GetNumberOfCellArrays()];

			//Cell data
			for (int i = 0; i < unstructuredGridReader.GetNumberOfCellArrays(); i++) 
			{
				dataArray[i] = unstructuredGridReader.GetCellArrayName(i) + " [C]";
			}
			
			//Point data
			for (int i = 0; i < unstructuredGridReader.GetNumberOfPointArrays(); i++) 
			{
				dataArray[unstructuredGridReader.GetNumberOfCellArrays() + i] = unstructuredGridReader.GetPointArrayName(i) + " [P]";
			}
		}
	}
}
