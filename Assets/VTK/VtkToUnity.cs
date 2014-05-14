using UnityEngine;
using System.Collections;

[System.Serializable]
public class VtkToUnity
{
	public Mesh mesh = new Mesh();
	public GameObject go;
	public Kitware.VTK.vtkTriangleFilter triangleFilter;
	public string name;
	public string colorFieldName = "";
	public VtkColorType colorDataType = VtkColorType.POINT_DATA;
	public Kitware.VTK.vtkDataArray colorArray = null;
	public Kitware.VTK.vtkLookupTable lut = Kitware.VTK.vtkLookupTable.New();
	public Material mat;
	public Color solidColor;

	public enum VtkColorType
	{
		POINT_DATA,
		CELL_DATA,
		SOLID_COLOR
	}

	public enum LutPreset
	{
		BLUE_RED,
		RED_BLUE,
		RAINBOW
	}
	public VtkToUnity(Kitware.VTK.vtkAlgorithmOutput outputPort, GameObject newGo)
	{
		name = newGo.name;
		triangleFilter = Kitware.VTK.vtkTriangleFilter.New();
		triangleFilter.SetInputConnection(outputPort);

		GameObject.DestroyImmediate(newGo.GetComponent<MeshFilter>());
		GameObject.DestroyImmediate(newGo.GetComponent<MeshRenderer>());

		go = newGo;

		MeshFilter meshFilter = go.AddComponent<MeshFilter> ();
		meshFilter.sharedMesh = mesh;
		go.AddComponent<MeshRenderer> ();

	}
	public VtkToUnity(Kitware.VTK.vtkAlgorithmOutput outputPort, string name)
	{
		this.name = name;
		triangleFilter = Kitware.VTK.vtkTriangleFilter.New();
		triangleFilter.SetInputConnection(outputPort);
		
		go = new GameObject(name);

		MeshFilter meshFilter = go.AddComponent<MeshFilter> ();
		meshFilter.sharedMesh = mesh;
		go.AddComponent<MeshRenderer> ();
	}

	~VtkToUnity()
	{
		/*
		foreach (Material mat in go.GetComponent<Renderer>().materials)
			Object.DestroyImmediate(mat);			
		*/
	}

	public void Update()
	{
		PolyDataToMesh();
	}

	void PolyDataToMesh()
	{
		// mesh.MarkDynamic();
		mesh.Clear();

		triangleFilter.Update();
		Kitware.VTK.vtkPolyData pd = triangleFilter.GetOutput();

		// Points / Vertices
		int numVertices = pd.GetNumberOfPoints();
		Vector3[] vertices = new Vector3[numVertices];
		for (int i = 0; i < numVertices; ++i)
		{
			double[] pnt = pd.GetPoint(i);
			// Flip z-up to y-up
			vertices[i] = new Vector3(-(float)pnt[0], (float)pnt[2], (float)pnt[1]);
		}
		mesh.vertices = vertices;

		// Triangles / Cells
		int numTriangles = pd.GetNumberOfPolys();
		int[] triangles = new int[numTriangles * 3];
		Kitware.VTK.vtkCellArray polys = pd.GetPolys();
		if (polys.GetNumberOfCells() > 0)
		{
			int prim = 0;
			Kitware.VTK.vtkIdList pts = Kitware.VTK.vtkIdList.New();
			polys.InitTraversal();
			while (polys.GetNextCell(pts) != 0)
			{
				for (int i = 0; i < pts.GetNumberOfIds(); ++i)
					triangles[prim * 3 + i] = pts.GetId(i);

				++prim;
			}
		}
		mesh.triangles = triangles;

		// Lines
		Kitware.VTK.vtkCellArray lines = pd.GetLines();
		if (lines.GetNumberOfCells() > 0)
		{
			Debug.LogWarning("lines");
			Kitware.VTK.vtkIdList pts = Kitware.VTK.vtkIdList.New();
			lines.InitTraversal();
			int prim = 0;
			while (lines.GetNextCell(pts) != 0)
			{
				Color lineColor = solidColor;
				if (colorArray != null)
				{
					if (colorDataType == VtkColorType.CELL_DATA)
						lineColor = GetColorAtIndex(prim);
					else if (colorDataType == VtkColorType.POINT_DATA)
						lineColor = GetColorAtIndex(pts.GetId(0));
				}

				Vector3[] linePoints = new Vector3[2];
				linePoints[0] = vertices[pts.GetId(0)];
				linePoints[1] = vertices[pts.GetId(1)];
				Vectrosity.VectorLine line = new Vectrosity.VectorLine(name + "-Line", 
					linePoints, lineColor, null, 50.0f);
				//line.Draw3DAuto(go.transform);
				line.Draw3D(go.transform);
				++prim;
			}
		}

		// Points
		Kitware.VTK.vtkCellArray points = pd.GetVerts();
		int numPointCells = points.GetNumberOfCells();
		if (numPointCells > 0)
		{
			ArrayList list = new ArrayList();
			ArrayList colorList = new ArrayList();
			Kitware.VTK.vtkIdList pts = Kitware.VTK.vtkIdList.New();
			points.InitTraversal();
			while (points.GetNextCell(pts) != 0)
			{
				ArrayList pointsList = new ArrayList();
				ArrayList colorsList = new ArrayList();
				for (int i = 0; i < pts.GetNumberOfIds(); ++i)
				{
					pointsList.Add(vertices[pts.GetId(i)]);
					Color pointColor = solidColor;
					if (colorArray != null && colorDataType == VtkColorType.POINT_DATA)
						pointColor = GetColorAtIndex(pts.GetId(i));
					colorsList.Add(pointColor);
				}
				list.AddRange(pointsList);
				colorList.AddRange(colorsList);
			}
			Vectrosity.VectorPoints pnt =
					new Vectrosity.VectorPoints(name + "-Point " + list.Count,
						list.ToArray(typeof(Vector3)) as Vector3[],
						colorList.ToArray(typeof(Color)) as Color[], null, 1f);
			//pnt.Draw3DAuto(go.transform);
			pnt.Draw3D(go.transform);
		}

		// Texture coordinates
		Vector2[] uvs;
		int numCoords = 0;
		Kitware.VTK.vtkDataArray vtkTexCoords = pd.GetPointData().GetTCoords();
		if (vtkTexCoords != null)
		{
			numCoords = vtkTexCoords.GetNumberOfTuples();
			uvs = new Vector2[numCoords];
			for (int i = 0; i < numCoords; ++i)
			{
				double[] texCoords = vtkTexCoords.GetTuple2(i);
				uvs[i] = new Vector2((float)texCoords[0], (float)texCoords[1]);
			}
			mesh.uv = uvs;
		}

		// Vertex colors
		if (numTriangles > 0 && colorArray != null)
		{
			Color32[] colors = new Color32[numVertices];

			for (int i = 0; i < numVertices; ++i)
				colors[i] = GetColor32AtIndex(i);

			mesh.colors32 = colors;
		}

		//Debug.Log("Number of point data arrays: " + pd.GetPointData().GetNumberOfArrays());
		//Debug.Log("  - " + pd.GetPointData().GetArrayName(0));
		//Debug.Log("Number of cell data arrays: " + pd.GetCellData().GetNumberOfArrays());
		//Debug.Log("  - " + pd.GetCellData().GetArrayName(0));
		//Debug.Log(name + " - Vertices: " + numPoints + ", triangle: " + numTriangles + ", UVs: " + numCoords);

		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
		//mesh.Optimize();
	}

	private byte[] GetByteColorAtIndex(int i)
	{
		double scalar = colorArray.GetTuple1(i);
		double[] dcolor = lut.GetColor(scalar);
		byte[] color = new byte[3];
		for (uint j = 0; j < 3; j++)
			color[j] = (byte)(255 * dcolor[j]);
		return color;
	}

	private Color32 GetColor32AtIndex(int i)
	{
		byte[] color = GetByteColorAtIndex(i);
		return new Color32(color[0], color[1], color[2], 255);
	}

	private Color GetColorAtIndex(int i)
	{
		return GetColor32AtIndex(i);
	}

	public void ColorBy(Color color)
	{
		colorFieldName = "";
		colorDataType = VtkColorType.SOLID_COLOR;
		solidColor = color;

		mat = new Material(Shader.Find("Diffuse"));
		mat.color = color;
		go.GetComponent<Renderer>().material = mat;
	}

	public void ColorBy(string fieldname, VtkColorType type)
	{
		colorFieldName = fieldname;
		colorDataType = type;

		if (colorFieldName != "")
		{
			triangleFilter.Update();
			Kitware.VTK.vtkPolyData pd = triangleFilter.GetOutput();

			if (colorDataType == VtkColorType.POINT_DATA)
				colorArray = pd.GetPointData().GetScalars(colorFieldName);
			else if (colorDataType == VtkColorType.CELL_DATA)
				colorArray = pd.GetCellData().GetScalars(colorFieldName);

			go.GetComponent<Renderer>().materials = new Material[2] { 
				new Material(Shader.Find("UFZ/Vertex Color Front")),
				new Material(Shader.Find("UFZ/Vertex Color Back"))};
		}
		else
		{
			colorArray = null;
			mat = new Material(Shader.Find("Diffuse"));
			mat.color = Color.magenta;
			go.GetComponent<Renderer>().material = mat;
			Debug.Log("Color array " + fieldname + " not found!");
		}
	}

	public void SetLut(LutPreset preset)
	{
		double [] range = {0.0, 1.0};
		if (colorArray != null)
			range = colorArray.GetRange();
		else
			Debug.Log("VtkToUnity.SetLut(): No color array set!");
		SetLut(preset, range[0], range[1]);
	}

	public void SetLut(LutPreset preset, double rangeMin, double rangeMax)
	{
		lut.SetTableRange(rangeMin, rangeMax);
		switch (preset)
		{
			case LutPreset.BLUE_RED:
				lut.SetHueRange(0.66, 1.0);
				lut.SetNumberOfColors(128);
				break;
			case LutPreset.RED_BLUE:
				lut.SetHueRange(1.0, 0.66);
				lut.SetNumberOfColors(128);
				//lut.SetNumberOfTableValues(2);
				//lut.SetTableValue(0, 1.0, 0.0, 0.0, 1.0);
				//lut.SetTableValue(1, 0.0, 0.0, 1.0, 1.0);
				break;
			case LutPreset.RAINBOW:
				lut.SetHueRange(0.0, 0.66);
				lut.SetNumberOfColors(256);
				break;
			default:
				break;
		}
		lut.Build();
	}
}
