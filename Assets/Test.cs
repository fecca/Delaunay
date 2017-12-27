using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Assets;
using MIConvexHull;
using UnityEngine.UI;
using Biome = Assets.VoronoiTile.Biomes;
using Random = UnityEngine.Random;

public class Test : MonoBehaviour
{
	[SerializeField]
	private Material planetMat;
	[SerializeField]
	private int layer;
	[SerializeField]
	private Materials materials = new Materials();

	private Text[] texts;
	private Text actionText;
	private Text toolText;
	private GameObject topColdGizmo;
	private GameObject botColdGizmo;
	private GameObject topTempGizmo;
	private GameObject botTempGizmo;
	private GameObject topWarmGizmo;
	private GameObject botWarmGizmo;
	private List<Vertex3> convexHullVertices;
	private List<Face3> convexHullFaces;
	private List<int> convexHullIndices;
	private List<Vector3> allVerts;
	private List<Vector3> vertsForVoronoiHull;
	private VoronoiMesh<Vertex3, Cell3, VoronoiEdge<Vertex3, Cell3>> voronoiMesh;
	private List<Vector3> waterVerts;
	private List<Vector3> landVerts;
	private List<TectonicPlate> plateList;
	private GameObject ocean;
	private float seconds = 1f;
	private double size = 5;
	private int numberOfVertices;
	private int numberOfTiles;
	private int seed;
	private bool oceanActive = true;

	public static Text biomeText;

	private int Plates { get; set; }
	private float RotationSpeed { get; set; }
	private float ColdLat { get; set; }
	private float TempLat { get; set; }
	private float WarmLat { get; set; }
	private float HumidityModifier { get; set; }
	private float LandAmount { get; set; }
	private int Seed
	{
		get { return seed; }
		set { seed = Convert.ToInt32(value); }
	}
	private int NumberOfVertices
	{
		get { return numberOfVertices; }
		set { numberOfVertices = Convert.ToInt32(value); }

	}

	private void Start()
	{
		numberOfVertices = 200;
		Plates = 20;
		RotationSpeed = 4;
		HumidityModifier = 0;
		ColdLat = 4.2f;
		TempLat = 2.7f;
		WarmLat = 0.9f;
		seed = Random.Range(Int32.MinValue, Int32.MaxValue);
		LandAmount = 4f;

		texts = FindObjectsOfType<Text>();
		biomeText = texts[12];

		Create();
	}
	private void Update()
	{
		transform.Rotate(Vector3.up * Time.deltaTime * RotationSpeed, Space.World);
		seconds -= Time.deltaTime;
		if (seconds <= 0)
		{
			Destroy(topWarmGizmo);
			Destroy(botWarmGizmo);
			Destroy(topColdGizmo);
			Destroy(botColdGizmo);
			Destroy(topTempGizmo);
			Destroy(botTempGizmo);
		}
	}
	private void LateUpdate()
	{
		if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
		{
			if (layer == 0) DetermineHeightBiomes();
			else if (layer == 1) SetMaterialToTemp();
			else if (layer == 2) SetMaterialToHum();
		}
	}

	public void DrawColdLat()
	{
		Destroy(topColdGizmo);
		Destroy(botColdGizmo);

		float h = Vector2.Distance(new Vector2(0, ColdLat), new Vector2(0, (float)size));
		float a = Mathf.Sqrt(h * (2 * (float)size - h));

		float h2 = Vector2.Distance(new Vector2(0, -ColdLat), new Vector2(0, (float)size));
		float a2 = Mathf.Sqrt(h2 * (2 * (float)size - h2));

		topColdGizmo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
		topColdGizmo.GetComponent<MeshRenderer>().material = materials.Cold;
		topColdGizmo.transform.localScale = new Vector3(a * 2 + 0.65f, 0.04f, a * 2 + 0.65f);
		topColdGizmo.transform.position = new Vector3(0, ColdLat, 0);

		botColdGizmo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
		botColdGizmo.GetComponent<MeshRenderer>().material = materials.Cold;
		botColdGizmo.transform.localScale = new Vector3(a2 * 2 + 0.65f, 0.04f, a2 * 2 + 0.65f);
		botColdGizmo.transform.position = new Vector3(0, -ColdLat, 0);
		seconds = 3f;
	}
	public void DrawTempLat()
	{
		Destroy(topTempGizmo);
		Destroy(botTempGizmo);

		float h = Vector2.Distance(new Vector2(0, TempLat), new Vector2(0, (float)size));
		float a = Mathf.Sqrt(h * (2 * (float)size - h));

		float h2 = Vector2.Distance(new Vector2(0, -TempLat), new Vector2(0, (float)size));
		float a2 = Mathf.Sqrt(h2 * (2 * (float)size - h2));

		topTempGizmo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
		topTempGizmo.GetComponent<MeshRenderer>().material = materials.Temperate;
		topTempGizmo.transform.localScale = new Vector3(a * 2 + 0.65f, 0.04f, a * 2 + 0.65f);
		topTempGizmo.transform.position = new Vector3(0, TempLat, 0);

		botTempGizmo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
		botTempGizmo.GetComponent<MeshRenderer>().material = materials.Temperate;
		botTempGizmo.transform.localScale = new Vector3(a2 * 2 + 0.65f, 0.04f, a2 * 2 + 0.65f);
		botTempGizmo.transform.position = new Vector3(0, -TempLat, 0);
		seconds = 3f;
	}
	public void DrawWarmLat()
	{
		Destroy(topWarmGizmo);
		Destroy(botWarmGizmo);

		float h = Vector2.Distance(new Vector2(0, WarmLat), new Vector2(0, (float)size));
		float a = Mathf.Sqrt(h * (2 * (float)size - h));

		float h2 = Vector2.Distance(new Vector2(0, -WarmLat), new Vector2(0, (float)size));
		float a2 = Mathf.Sqrt(h2 * (2 * (float)size - h2));

		topWarmGizmo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
		topWarmGizmo.GetComponent<MeshRenderer>().material = materials.Warm;
		topWarmGizmo.transform.localScale = new Vector3(a * 2 + 0.65f, 0.04f, a * 2 + 0.65f);
		topWarmGizmo.transform.position = new Vector3(0, WarmLat, 0);

		botWarmGizmo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
		botWarmGizmo.GetComponent<MeshRenderer>().material = materials.Warm;
		botWarmGizmo.transform.localScale = new Vector3(a2 * 2 + 0.65f, 0.04f, a2 * 2 + 0.65f);
		botWarmGizmo.transform.position = new Vector3(0, -WarmLat, 0);
		seconds = 3f;
	}
	public void Create()
	{
		foreach (Transform child in transform)
		{
			Destroy(child.gameObject);
		}

		InputField[] fields = FindObjectsOfType<InputField>();
		InputField numseed = fields[0];
		InputField numplates = fields[1];
		InputField numtiles = fields[2];
		numberOfVertices = Convert.ToInt32(numtiles.text);
		Plates = Convert.ToInt32(numplates.text);
		seed = Convert.ToInt32(numseed.text);

		// INITIALIZATION
		Vertex3[] vertices = new Vertex3[numberOfVertices];
		Vector3[] meshVerts = new Vector3[numberOfVertices];
		allVerts = new List<Vector3>();
		vertsForVoronoiHull = new List<Vector3>();

		// VORONOI VERTICES NEED ONE EXTRA ONE IN CENTER
		Vertex3[] voronoiVertices = new Vertex3[numberOfVertices + 1];

		// RANDOM SEED
		Random.seed = seed;

		// GENERATE UNIFORM POINTS
		allVerts = GeneratePointsUniformly();
		allVerts.Sort((v1, v2) => v1.y.CompareTo(v2.y));

		// SET INDICES FOR VORONOI
		int i = 0;
		while (i < numberOfVertices)
		{
			vertices[i] = new Vertex3(allVerts[i].x, allVerts[i].y, allVerts[i].z);
			voronoiVertices[i] = vertices[i];
			meshVerts[i] = vertices[i].ToVector3();
			i++;
		}
		// SET LAST EXTRA VERTEX
		voronoiVertices[numberOfVertices] = new Vertex3(0, 0, 0);


		// VORONOI
		voronoiMesh = VoronoiMesh.Create<Vertex3, Cell3>(voronoiVertices);



		// VORONOI HULL GENERATION
		int index = 0;
		foreach (var edge in voronoiMesh.Edges)
		{
			Vector3 source = new Vector3(edge.Source.Circumcenter.x, edge.Source.Circumcenter.y, edge.Source.Circumcenter.z);
			Vector3 target = new Vector3(edge.Target.Circumcenter.x, edge.Target.Circumcenter.y, edge.Target.Circumcenter.z);
			source *= ((float)size / 2.5f);
			target *= ((float)size / 2.5f);
			vertsForVoronoiHull.Add(source);
			vertsForVoronoiHull.Add(target);
			index++;
		}

		// REMOVE DUPLICATE POINTS
		vertsForVoronoiHull = vertsForVoronoiHull.Distinct().ToList();

		// CONVERT FROM VECTOR3 LIST TO VERTEX3 LIST FOR FINAL HULL
		Vertex3[] verticesDelaunay = new Vertex3[vertsForVoronoiHull.Count];

		int g = 0;
		while (g < vertsForVoronoiHull.Count)
		{
			verticesDelaunay[g] = new Vertex3(vertsForVoronoiHull[g].x, vertsForVoronoiHull[g].y, vertsForVoronoiHull[g].z);
			g++;
		}

		// GENERATE VORONOI HULL
		ConvexHull<Vertex3, Face3> convexHull = ConvexHull.Create<Vertex3, Face3>(verticesDelaunay);
		convexHullVertices = new List<Vertex3>(convexHull.Points);
		convexHullFaces = new List<Face3>(convexHull.Faces);
		convexHullIndices = new List<int>();

		foreach (Face3 f in convexHullFaces)
		{
			convexHullIndices.Add(convexHullVertices.IndexOf(f.Vertices[0]));
			convexHullIndices.Add(convexHullVertices.IndexOf(f.Vertices[1]));
			convexHullIndices.Add(convexHullVertices.IndexOf(f.Vertices[2]));
		}

		Dictionary<Vector3, List<Vector3>> normals = new Dictionary<Vector3, List<Vector3>>();

		// CREATE TRIANGLES FOR MESH
		for (int j = 0; j < convexHullIndices.Count; j += 3)
		{
			int v0 = convexHullIndices[j + 0];
			int v1 = convexHullIndices[j + 1];
			int v2 = convexHullIndices[j + 2];

			Vector3 a = new Vector3((float)convexHullVertices[v0].x, (float)convexHullVertices[v0].y, (float)convexHullVertices[v0].z);
			Vector3 b = new Vector3((float)convexHullVertices[v1].x, (float)convexHullVertices[v1].y, (float)convexHullVertices[v1].z);
			Vector3 c = new Vector3((float)convexHullVertices[v2].x, (float)convexHullVertices[v2].y, (float)convexHullVertices[v2].z);

			Vector3 normal = Vector3.Cross(a - b, a - c);

			// DECLARE KEY AND ROUND IT TO AVOID FLOATING POINT ISSUES
			Vector3 key = normal.normalized;
			float roundX = Mathf.Round(key.x * 100) / 100;
			float roundY = Mathf.Round(key.y * 100) / 100;
			float roundZ = Mathf.Round(key.z * 100) / 100;
			Vector3 roundedKey = new Vector3(roundX, roundY, roundZ);

			// POPULATE DICTIONARY
			if (!normals.ContainsKey(roundedKey))
			{
				normals.Add(roundedKey, new List<Vector3>());
			}
			normals[roundedKey].Add(a);
			normals[roundedKey].Add(b);
			normals[roundedKey].Add(c);
		}

		// CREATE VORONOI TILES
		List<VoronoiTile> tiles = new List<VoronoiTile>();
		foreach (var pair in normals)
		{
			List<Vector3> tileVerts = new List<Vector3>();
			for (int p = 0; p < pair.Value.Count; ++p)
			{
				tileVerts.Add(pair.Value[p]);
			}
			GameObject tile = new GameObject("Tile", typeof(VoronoiTile), typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
			var thisTile = tile.GetComponent<VoronoiTile>() as VoronoiTile;

			thisTile.Initialize(tileVerts, false); // OPTIMIZE HERE

			tile.GetComponent<MeshFilter>().mesh = thisTile.tileMesh;
			tile.GetComponent<MeshCollider>().sharedMesh = thisTile.tileMesh;

			thisTile.Normal = pair.Key;
			tiles.Add(thisTile);
			++numberOfTiles;
		}

		foreach (var tile in tiles)
		{
			tile.FindNeighbors(tiles);
		}

		List<VoronoiTile> waterTiles = new List<VoronoiTile>();
		foreach (var pair in normals)
		{
			List<Vector3> tileVerts = new List<Vector3>();
			for (int p = 0; p < pair.Value.Count; ++p)
			{
				tileVerts.Add(pair.Value[p]);
			}
			GameObject tile = new GameObject("WaterTile", typeof(VoronoiTile), typeof(MeshFilter), typeof(MeshRenderer));
			var thisTile = tile.GetComponent<VoronoiTile>() as VoronoiTile;
			thisTile.Initialize(tileVerts, true);
			tile.GetComponent<MeshFilter>().mesh = thisTile.tileMesh;
			tile.GetComponent<MeshRenderer>().material = materials.Ocean;
			waterTiles.Add(thisTile);
		}

		ocean = new GameObject("Ocean");
		ocean.transform.parent = transform;
		foreach (var tile in waterTiles)
		{
			tile.transform.parent = ocean.transform;
		}

		// FLOOD FILLS
		// GENERATE PLATES
		List<VoronoiTile> plateStartNodes = GeneratePlateStartNodes(Plates, ref tiles);

		// GENERATE PLATE MATERIALS
		List<Material> plateMaterials = GenerateMaterials(1); // changed here

		// GENERATE START LISTS
		List<List<VoronoiTile>> colors = new List<List<VoronoiTile>>();
		for (int b = 0; b < Plates; ++b)
		{
			colors.Add(new List<VoronoiTile>() { plateStartNodes[b] });
		}

		// FILL
		FloodFillSimultaneous(ref colors, plateMaterials, ref tiles);

		// GROUP PLATES
		plateList = new List<TectonicPlate>();
		for (int q = 0; q < Plates; ++q)
		{
			GameObject plateTest = new GameObject("Plate" + q, typeof(TectonicPlate));
			List<VoronoiTile> testPlateTiles = new List<VoronoiTile>();
			foreach (var voronoiTile in tiles)
			{
				if (voronoiTile.plate == q) testPlateTiles.Add(voronoiTile);
			}
			var thisTecPlate = plateTest.GetComponent<TectonicPlate>();
			thisTecPlate.Initialize(ref testPlateTiles);
			int land = Random.Range(0, 10);
			if (land < LandAmount) thisTecPlate.isLand = true;
			plateTest.transform.parent = transform;
			plateList.Add(thisTecPlate);
		}

		// CREATE WATER AND LAND AREAS FOR HEIGHT
		FindWaterAndLandPoints();

		// DETERMINE BIOMES
		AssignPlateProperties();
		AssignTileProperties();
		DetermineBiomes(true);
		GenerateHeight();
		DetermineHeightBiomes();
	}
	public void SetMaterialToBlank()
	{
		foreach (var plate in plateList)
		{
			foreach (var tile in plate.tiles)
			{
				tile.GetComponent<MeshRenderer>().material = materials.Blank;
			}
		}
	}
	public void SetMaterialToTemp()
	{
		layer = 1;
		foreach (var plate in plateList)
		{
			foreach (var tile in plate.tiles)
			{
				if (tile.temperature > 0.0f && tile.temperature <= 1f) tile.GetComponent<MeshRenderer>().material = materials.Temperature1;
				else if (tile.temperature > 1f && tile.temperature <= 2f) tile.GetComponent<MeshRenderer>().material = materials.Temperature2;
				else if (tile.temperature > 2f && tile.temperature <= 3f) tile.GetComponent<MeshRenderer>().material = materials.Temperature3;
				else if (tile.temperature > 3f) tile.GetComponent<MeshRenderer>().material = materials.Temperature4;
			}
		}
	}
	public void SetMaterialToHum()
	{
		layer = 2;
		foreach (var plate in plateList)
		{
			foreach (var tile in plate.tiles)
			{
				if (tile.humidity > -2.0f && tile.humidity <= 1f) tile.GetComponent<MeshRenderer>().material = materials.Humidity1;
				else if (tile.humidity > 1f && tile.humidity <= 2f) tile.GetComponent<MeshRenderer>().material = materials.Humidity2;
				else if (tile.humidity > 2f && tile.humidity <= 3f) tile.GetComponent<MeshRenderer>().material = materials.Humidity3;
				else if (tile.humidity > 3f && tile.humidity <= 4f) tile.GetComponent<MeshRenderer>().material = materials.Humidity4;
			}
		}
	}
	public void SetMaterialToHeight()
	{
		layer = 3;
		foreach (var plate in plateList)
		{
			foreach (var tile in plate.tiles)
			{
				if (tile.altitude > 0.0f) tile.GetComponent<MeshRenderer>().material = materials.Altitude1;
				if (tile.altitude > 0.02f) tile.GetComponent<MeshRenderer>().material = materials.Altitude2;
				if (tile.altitude > 0.04f) tile.GetComponent<MeshRenderer>().material = materials.Altitude3;
			}
		}
	}
	public void ToggleOcean()
	{
		oceanActive = !oceanActive;
		ocean.SetActive(oceanActive);
	}
	public void Export()
	{
		GameObject generator = gameObject;

		Combiner.CombineMeshes(generator);
		ObjExporter.MeshesToFile(generator.GetComponents<MeshFilter>(), Application.dataPath, "Planet");
	}
	public void DetermineHeightBiomes()
	{
		layer = 0;
		foreach (var plate in plateList)
		{
			foreach (var tile in plate.tiles)
			{
				if (tile.altitude > 0.02f) { tile.GetComponent<MeshRenderer>().material = materials.Hill; }
				if (tile.altitude > 0.04f) { tile.GetComponent<MeshRenderer>().material = materials.Mountain; }
				if (tile.altitude <= 0.00f) { tile.GetComponent<MeshRenderer>().material = materials.Sand1; }
				if (tile.altitude < -0.02f) tile.GetComponent<MeshRenderer>().material = materials.Sand2;
				if (tile.altitude < -0.04f) tile.GetComponent<MeshRenderer>().material = materials.Sand3;
				if (tile.altitude < -0.06f) tile.GetComponent<MeshRenderer>().material = materials.Sand4;
				if (tile.altitude < -0.14f) tile.GetComponent<MeshRenderer>().material = materials.Lava;
				if (Mathf.Approximately(tile.altitude, 0.02f))
				{
					if (tile.biome == Biome.Sand) tile.GetComponent<MeshRenderer>().material = materials.Sand1;
					else if (tile.biome == Biome.Glacier) tile.GetComponent<MeshRenderer>().material = materials.Glacier;
					else if (tile.biome == Biome.Plains) tile.GetComponent<MeshRenderer>().material = materials.Plains;
					else if (tile.biome == Biome.Snow) tile.GetComponent<MeshRenderer>().material = materials.Snow;
					else if (tile.biome == Biome.Jungle) tile.GetComponent<MeshRenderer>().material = materials.Jungle;
					else if (tile.biome == Biome.Desert) tile.GetComponent<MeshRenderer>().material = materials.Desert;
					else if (tile.biome == Biome.Dirt) tile.GetComponent<MeshRenderer>().material = materials.Dirt;
					else if (tile.biome == Biome.Tundra) tile.GetComponent<MeshRenderer>().material = materials.Tundra;
					else if (tile.biome == Biome.Forest) tile.GetComponent<MeshRenderer>().material = materials.Forest;
				}
				tile.GetComponent<MeshCollider>().sharedMesh = tile.tileMesh;
				tile.GetComponent<MeshCollider>().convex = true;
			}
		}
	}
	public void RecalculateBiomes()
	{
		AssignPlateProperties();
		AssignTileProperties();
		DetermineBiomes(false);
		if (layer == 0) DetermineHeightBiomes();
		else if (layer == 1) SetMaterialToTemp();
		else if (layer == 2) SetMaterialToHum();
		else if (layer == 3) SetMaterialToHeight();
	}

	private void UnifyMesh(ref Mesh mesh, ref List<Material> mats, Transform t)
	{
		// IT ONLY TAKES ONE MESH FILTER PER PLATE
		MeshFilter[] meshFilters = t.GetComponentsInChildren<MeshFilter>();
		CombineInstance[] combine = new CombineInstance[meshFilters.Length];
		var index = 0;

		for (int i = 0; i < meshFilters.Length; ++i)
		{
			if (meshFilters[i].sharedMesh == null) continue;
			combine[index].mesh = meshFilters[i].sharedMesh;
			combine[index++].transform = meshFilters[i].transform.localToWorldMatrix;
			Material mat = meshFilters[i].gameObject.GetComponent<MeshRenderer>().sharedMaterial;
			mats.Add(mat);
		}
		mesh.CombineMeshes(combine, false);
	}
	private void FindWaterAndLandPoints()
	{
		waterVerts = new List<Vector3>();
		landVerts = new List<Vector3>();
		foreach (var tectonicPlate in plateList)
		{
			if (!tectonicPlate.isLand)
			{
				foreach (var voronoiTile in tectonicPlate.tiles)
				{
					foreach (var vertex in voronoiTile.tileMesh.vertices)
					{
						waterVerts.Add(vertex);
					}
				}
			}
			else
			{
				foreach (var voronoiTile in tectonicPlate.tiles)
				{
					foreach (var vertex in voronoiTile.tileMesh.vertices)
					{
						landVerts.Add(vertex);
					}
				}
			}
		}
		waterVerts = waterVerts.Distinct().ToList();
		landVerts = landVerts.Distinct().ToList();
	}
	private void AssignPlateProperties()
	{
		foreach (var tectonicPlate in plateList)
		{
			float distanceFromEquator = tectonicPlate.middle.y;
			if (distanceFromEquator > ColdLat || distanceFromEquator < -ColdLat) // POLES
			{
				tectonicPlate.SetTemp(0.5f);
				tectonicPlate.SetHumidity(tectonicPlate.baseHumidity);
			}
			else if (distanceFromEquator < ColdLat && distanceFromEquator > TempLat || distanceFromEquator > -ColdLat && distanceFromEquator < -TempLat) // COLD
			{
				tectonicPlate.SetTemp(1.5f);
				tectonicPlate.SetHumidity(tectonicPlate.baseHumidity);
			}
			else if (distanceFromEquator < TempLat && distanceFromEquator > WarmLat || distanceFromEquator > -TempLat && distanceFromEquator < -WarmLat) // TEMPERED
			{
				tectonicPlate.SetTemp(2.5f);
				tectonicPlate.SetHumidity(tectonicPlate.baseHumidity);
			}
			else if (distanceFromEquator < WarmLat && distanceFromEquator >= 0.0 || distanceFromEquator > -WarmLat && distanceFromEquator <= 0.0) // WARM
			{
				tectonicPlate.SetTemp(3.5f);
				tectonicPlate.SetHumidity(tectonicPlate.baseHumidity);
			}
		}
	}
	private void AssignTileProperties()
	{
		foreach (var tectonicPlate in plateList)
		{
			foreach (var tile in tectonicPlate.tiles)
			{
				float distanceFromEquator = tile.centerPoint.y;
				if (distanceFromEquator > ColdLat || distanceFromEquator < -ColdLat)
				{
					tile.temperature = (tile.temperature + 0.5f) / 2;
				}
				else if (distanceFromEquator < ColdLat && distanceFromEquator > TempLat || distanceFromEquator > -ColdLat && distanceFromEquator < -TempLat)
				{
					tile.temperature = (tile.temperature + 1.5f) / 2;
				}
				else if (distanceFromEquator < TempLat && distanceFromEquator > WarmLat || distanceFromEquator > -TempLat && distanceFromEquator < -WarmLat)
				{
					tile.temperature = (tile.temperature + 2.5f) / 2;
				}
				else if (distanceFromEquator < WarmLat && distanceFromEquator > 0.0 || distanceFromEquator > -WarmLat && distanceFromEquator < 0.0)
				{
					tile.temperature = (tile.temperature + 3.5f) / 2;
				}
				tile.humidity += HumidityModifier;
			}
		}
	}
	private void DetermineBiomes(bool alsoWater)
	{
		foreach (var tectonicPlate in plateList)
		{
			foreach (var tile in tectonicPlate.tiles)
			{
				tile.DetermineBiome();
				if (alsoWater) tile.DetermineBaseBiome();
				if (tile.biome == Biome.Sand) tile.GetComponent<MeshRenderer>().material = materials.Sand1;
				else if (tile.biome == Biome.Glacier) tile.GetComponent<MeshRenderer>().material = materials.Glacier;
				else if (tile.biome == Biome.Plains) tile.GetComponent<MeshRenderer>().material = materials.Plains;
				else if (tile.biome == Biome.Snow) tile.GetComponent<MeshRenderer>().material = materials.Snow;
				else if (tile.biome == Biome.Jungle) tile.GetComponent<MeshRenderer>().material = materials.Jungle;
				else if (tile.biome == Biome.Desert) tile.GetComponent<MeshRenderer>().material = materials.Desert;
				else if (tile.biome == Biome.Dirt) tile.GetComponent<MeshRenderer>().material = materials.Dirt;
				else if (tile.biome == Biome.Tundra) tile.GetComponent<MeshRenderer>().material = materials.Tundra;
				else if (tile.biome == Biome.Forest) tile.GetComponent<MeshRenderer>().material = materials.Forest;
				else tile.GetComponent<MeshRenderer>().material = materials.Blank;
			}
		}
	}
	private void GenerateHeight()
	{
		foreach (var tectonicPlate in plateList)
		{
			if (tectonicPlate.isLand)
			{
				tectonicPlate.PushOutLand(0.02f);
				foreach (var tile in tectonicPlate.tiles)
				{
					float distance = Vector3.Distance(tile.centerPoint, FindClosest(tile.centerPoint, waterVerts));
					if (distance > 1.3f)
					{
						tile.Push(0.02f);
						if (distance > 2)
						{
							tile.Push(0.02f);
						}
					}

					tile.GetComponent<MeshCollider>().sharedMesh = tile.tileMesh;
					tile.GetComponent<MeshCollider>().convex = true;
				}
			}
			else
			{
				tectonicPlate.PushOutLand(-0.02f);
				foreach (var tile in tectonicPlate.tiles)
				{
					float distance = Vector3.Distance(tile.centerPoint, FindClosest(tile.centerPoint, landVerts));
					if (distance > 1)
					{
						tile.Push(-0.02f);
						if (distance > 2)
						{
							tile.Push(-0.02f);
							if (distance > 3)
							{
								tile.Push(-0.02f);
							}
						}
					}
					tile.GetComponent<MeshCollider>().sharedMesh = tile.tileMesh;
					tile.GetComponent<MeshCollider>().convex = true;
				}
			}
		}
	}
	private void GenerateBestCandidates(ref List<Vector3> samples, float s, int depth)
	{
		if (depth == 1) return;
		List<Vector3> candidates = new List<Vector3>();
		for (int i = 0; i < 10; ++i)
		{
			Vector3 point = Random.onUnitSphere;
			point *= s;
			candidates.Add(point);
		}

		Vector3 bestCandidate = Vector3.zero;
		bool isFirst = true;
		float largestDistance = 0;
		foreach (var candidate in candidates)
		{
			Vector3 closest = FindClosest(candidate, samples);
			float distance = Vector3.Distance(closest, candidate);

			if (isFirst || distance > largestDistance)
			{
				largestDistance = distance;
				bestCandidate = candidate;
				isFirst = false;
			}
		}
		samples.Add(bestCandidate);
		GenerateBestCandidates(ref samples, s, depth - 1);
	}
	private void NonRecursiveGenerateBestCandidates(ref List<Vector3> samples, float s)
	{
		while (samples.Count < numberOfVertices)
		{
			List<Vector3> candidates = new List<Vector3>();
			for (int i = 0; i < 10; ++i)
			{
				Vector3 point = Random.onUnitSphere;
				point *= s;
				candidates.Add(point);
			}

			Vector3 bestCandidate = Vector3.zero;
			bool isFirst = true;
			float largestDistance = 0;
			foreach (var candidate in candidates)
			{
				Vector3 closest = FindClosest(candidate, samples);
				float distance = Vector3.Distance(closest, candidate);

				if (isFirst || distance > largestDistance)
				{
					largestDistance = distance;
					bestCandidate = candidate;
					isFirst = false;
				}
			}
			samples.Add(bestCandidate);
		}

	}
	private void GenerateBestTileCandidates(ref List<VoronoiTile> samples, int depth, ref List<VoronoiTile> allTiles)
	{
		if (depth == 1) return;
		List<VoronoiTile> candidates = new List<VoronoiTile>();
		for (int i = 0; i < 10; ++i)
		{
			VoronoiTile tile = allTiles[Random.Range(0, allTiles.Count)];
			candidates.Add(tile);
		}
		VoronoiTile bestCandidate = candidates.First();
		bool isFirst = true;
		float largestDistance = 0;
		foreach (var candidate in candidates)
		{
			VoronoiTile closest = FindClosestTile(candidate, ref samples);
			float distance = Vector3.Distance(closest.centerPoint, candidate.centerPoint);
			if (isFirst || distance > largestDistance)
			{
				largestDistance = distance;
				bestCandidate = candidate;
				isFirst = false;
			}
		}
		samples.Add(bestCandidate);
		GenerateBestTileCandidates(ref samples, depth - 1, ref allTiles);
	}
	private void FillNeighbors(VoronoiTile node, Material replacement, int plateNumber)
	{
		if (node.processed == false)
		{
			node.GetComponent<MeshRenderer>().material = replacement;
			node.plate = plateNumber;
			node.processed = true;
		}
		foreach (var neighbor in node.neighbors)
		{
			if (neighbor.processed == false)
			{
				neighbor.GetComponent<MeshRenderer>().material = replacement;
				neighbor.plate = plateNumber;
				neighbor.processed = true;
			}
		}
	}
	private void FloodFillSimultaneous(ref List<List<VoronoiTile>> colors, List<Material> replacements, ref List<VoronoiTile> allTiles)
	{
		while (!AreAllProcessed(ref allTiles))
		{
			for (int colorIndex = 0; colorIndex < colors.Count; ++colorIndex) // for each node in colors, fill neighbors
			{
				List<VoronoiTile> newNodes = new List<VoronoiTile>();
				for (int tileIndex = 0; tileIndex < colors[colorIndex].Count; tileIndex++) // go over each node in the colors (first step only one) and fill neighbors
				{
					VoronoiTile tile = colors[colorIndex][tileIndex];
					FillNeighbors(tile, replacements[0], colorIndex); // changed here
					foreach (var neighbor in tile.neighbors)
					{
						foreach (var nbr in neighbor.neighbors)
						{
							if (!nbr.processed) newNodes.Add(nbr);
						}
					}
				}
				colors[colorIndex].Clear();
				colors[colorIndex] = newNodes;
			}
		}
	}
	private bool AreAllProcessed(ref List<VoronoiTile> allTiles)
	{
		foreach (var tile in allTiles)
		{
			if (!tile.processed) return false;
		}
		return true;
	}
	private Vector3 FindClosest(Vector3 point, List<Vector3> samples)
	{
		Vector3 closest = new Vector3();
		bool isFirst = true;
		float smallestDistance = 0;
		foreach (var p in samples)
		{
			float distance = Vector3.Distance(p, point);
			if (isFirst || distance < smallestDistance)
			{
				smallestDistance = distance;
				closest = p;
				isFirst = false;
			}
		}
		return closest;
	}
	private List<Vector3> FindClosestTwo(List<Vector3> firstSet, List<Vector3> secondSet)
	{
		List<Vector3> closestTwo = new List<Vector3>();
		bool isFirst = true;
		float smallestDistance = 0;
		foreach (var pos in firstSet)
		{
			Vector3 other = FindClosest(pos, secondSet);
			float distance = Vector3.Distance(pos, other);
			if (isFirst || distance < smallestDistance)
			{
				closestTwo.Clear();
				smallestDistance = distance;
				closestTwo.Add(pos);
				closestTwo.Add(other);
				isFirst = false;
			}
		}
		return closestTwo;
	}
	private List<VoronoiTile> GeneratePlateStartNodes(int count, ref List<VoronoiTile> allTiles)
	{
		VoronoiTile firstTile = allTiles[Random.Range(0, allTiles.Count)];
		List<VoronoiTile> nodes = new List<VoronoiTile>();
		nodes.Add(firstTile);
		GenerateBestTileCandidates(ref nodes, count, ref allTiles);
		return nodes;
	}
	private VoronoiTile FindClosestTile(VoronoiTile tile, ref List<VoronoiTile> samples)
	{
		VoronoiTile closest = tile;
		bool isFirst = true;
		float smallestDistance = 0;
		foreach (var sample in samples)
		{
			float distance = Vector3.Distance(sample.centerPoint, tile.centerPoint);
			if (isFirst || distance < smallestDistance)
			{
				smallestDistance = distance;
				closest = sample;
				isFirst = false;
			}
		}
		return closest;
	}
	private List<Material> GenerateMaterials(int amount)
	{
		List<Material> materials = new List<Material>();
		for (int i = 0; i < amount; i++)
		{
			var material = new Material(Shader.Find("Standard"));
			material.SetFloat("_Glossiness", 0.0f);
			material.color = new Color(Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f), 1);
		}
		for (int j = 0; j < amount; ++j)
		{
			materials.Add(Resources.Load("" + j, typeof(Material)) as Material);
		}
		return materials;
	}
	private Mesh GenerateTriangle(Vector3 a, Vector3 b, Vector3 c)
	{
		Mesh tri = new Mesh();
		tri.vertices = new Vector3[] { a, b, c };
		tri.triangles = new int[] { 0, 1, 2 };
		tri.RecalculateNormals();
		tri.RecalculateBounds();
		;
		return tri;
	}
	private List<Vector3> GeneratePointsUniformly()
	{
		float s = (float)size;
		Vector3 firstPoint = Random.onUnitSphere;
		firstPoint *= s;
		List<Vector3> bestCandidates = new List<Vector3>();
		bestCandidates.Add(firstPoint);
		NonRecursiveGenerateBestCandidates(ref bestCandidates, s);
		return bestCandidates;
	}
}




















