﻿using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Assets;
using MIConvexHull;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class Generate : MonoBehaviour
{
	public int Size = 5;
	public int NumberOfPoints = 200;
	public int NumberOfCandidates = 10;
	public int Plates = 30;
	public int Seed = 0;
	public Materials Materials = new Materials();

	private List<Vector3> m_waterVerts;
	private List<Vector3> m_landVerts;
	private List<TectonicPlate> m_tectonicPlates;
	private GameObject m_topColdGizmo;
	private GameObject m_botColdGizmo;
	private GameObject m_topTempGizmo;
	private GameObject m_botTempGizmo;
	private GameObject m_topWarmGizmo;
	private GameObject m_botWarmGizmo;
	private GameObject m_ocean;
	private Text[] m_texts;
	private InputField[] m_inputFields;
	private InputField m_seedField;
	private InputField m_platesField;
	private InputField m_tilesField;
	private bool m_oceanActive = true;
	private float m_seconds = 1f;
	private int m_numberOfTiles;
	private int m_layer;

	public static Text biomeText;

	public float LandAmount { get; set; }
	public float RotationSpeed { get; set; }
	public float HumidityModifier { get; set; }
	public float ColdLat { get; set; }
	public float TempLat { get; set; }
	public float WarmLat { get; set; }

	private void Start()
	{
		m_inputFields = FindObjectsOfType<InputField>();
		m_seedField = m_inputFields[0];
		m_platesField = m_inputFields[1];
		m_tilesField = m_inputFields[2];
		m_texts = FindObjectsOfType<Text>();
		biomeText = m_texts[12];

		RotationSpeed = 4;
		HumidityModifier = 0;
		ColdLat = 4.2f;
		TempLat = 2.7f;
		WarmLat = 0.9f;
		LandAmount = 4f;

		Create();
	}
	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Return))
		{
			Create();
		}

		transform.Rotate(Vector3.up * Time.deltaTime * RotationSpeed, Space.World);
		m_seconds -= Time.deltaTime;
		if (m_seconds <= 0)
		{
			Destroy(m_topWarmGizmo);
			Destroy(m_botWarmGizmo);
			Destroy(m_topColdGizmo);
			Destroy(m_botColdGizmo);
			Destroy(m_topTempGizmo);
			Destroy(m_botTempGizmo);
		}
	}
	private void LateUpdate()
	{
		if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
		{
			if (m_layer == 0)
			{
				DetermineHeightBiomes();
			}
			else if (m_layer == 1)
			{
				SetMaterialToTemp();
			}
			else if (m_layer == 2)
			{
				SetMaterialToHum();
			}
		}
	}
	private void OnDrawGizmos()
	{
	}

	public void Create()
	{
		if (!Application.isPlaying)
		{
			return;
		}

		ResetWorld();

		var allVertices = GeneratePointsUniformly();
		var voronoiVertices = GenerateVoronoiVertices(allVertices);
		var voronoiMesh = VoronoiMesh.Create<Vertex3, Cell3>(voronoiVertices);
		var voronoiHullVertices = GenerateVoronoiHullVertices(voronoiMesh);
		var verticesDelaunay = GenerateDelaunayVertices(voronoiHullVertices);
		var convexHull = ConvexHull.Create<Vertex3, Face3>(verticesDelaunay);
		var convexHullVertices = new List<Vertex3>(convexHull.Points);
		var convexHullFaces = new List<Face3>(convexHull.Faces);
		var convexHullIndices = GenerateConvexHullIndices(convexHullFaces, convexHullVertices);
		var normals = GenerateNormals(convexHullVertices, convexHullIndices);
		var tiles = CreateTiles(normals);

		CreateWaterTiles(normals);
		FindTileNeighbours(tiles);

		var plateStartNodes = GeneratePlateStartNodes(Plates, tiles);
		var plateMaterials = GenerateMaterials(1);

		FloodFillSimultaneous(plateStartNodes, plateMaterials, tiles);
		GroupPlates(tiles);
		FindWaterAndLandPoints();
		AssignPlateProperties();
		AssignTileProperties();
		DetermineBiomes(true);
		GenerateHeight();
		DetermineHeightBiomes();

		foreach (var tile in tiles)
		{
			tile.GetComponent<MeshCollider>().sharedMesh = tile.tileMesh;
			tile.GetComponent<MeshCollider>().convex = true;
		}
	}
	public void DrawColdLat()
	{
		Destroy(m_topColdGizmo);
		Destroy(m_botColdGizmo);

		float h = Vector2.Distance(new Vector2(0, ColdLat), new Vector2(0, Size));
		float a = Mathf.Sqrt(h * (2 * Size - h));

		float h2 = Vector2.Distance(new Vector2(0, -ColdLat), new Vector2(0, Size));
		float a2 = Mathf.Sqrt(h2 * (2 * Size - h2));

		m_topColdGizmo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
		m_topColdGizmo.GetComponent<MeshRenderer>().sharedMaterial = Materials.Cold;
		m_topColdGizmo.transform.localScale = new Vector3(a * 2 + 0.65f, 0.04f, a * 2 + 0.65f);
		m_topColdGizmo.transform.position = new Vector3(0, ColdLat, 0);

		m_botColdGizmo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
		m_botColdGizmo.GetComponent<MeshRenderer>().sharedMaterial = Materials.Cold;
		m_botColdGizmo.transform.localScale = new Vector3(a2 * 2 + 0.65f, 0.04f, a2 * 2 + 0.65f);
		m_botColdGizmo.transform.position = new Vector3(0, -ColdLat, 0);
		m_seconds = 3f;
	}
	public void DrawTempLat()
	{
		Destroy(m_topTempGizmo);
		Destroy(m_botTempGizmo);

		float h = Vector2.Distance(new Vector2(0, TempLat), new Vector2(0, Size));
		float a = Mathf.Sqrt(h * (2 * Size - h));

		float h2 = Vector2.Distance(new Vector2(0, -TempLat), new Vector2(0, Size));
		float a2 = Mathf.Sqrt(h2 * (2 * Size - h2));

		m_topTempGizmo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
		m_topTempGizmo.GetComponent<MeshRenderer>().sharedMaterial = Materials.Temperate;
		m_topTempGizmo.transform.localScale = new Vector3(a * 2 + 0.65f, 0.04f, a * 2 + 0.65f);
		m_topTempGizmo.transform.position = new Vector3(0, TempLat, 0);

		m_botTempGizmo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
		m_botTempGizmo.GetComponent<MeshRenderer>().sharedMaterial = Materials.Temperate;
		m_botTempGizmo.transform.localScale = new Vector3(a2 * 2 + 0.65f, 0.04f, a2 * 2 + 0.65f);
		m_botTempGizmo.transform.position = new Vector3(0, -TempLat, 0);
		m_seconds = 3f;
	}
	public void DrawWarmLat()
	{
		Destroy(m_topWarmGizmo);
		Destroy(m_botWarmGizmo);

		float h = Vector2.Distance(new Vector2(0, WarmLat), new Vector2(0, Size));
		float a = Mathf.Sqrt(h * (2 * Size - h));

		float h2 = Vector2.Distance(new Vector2(0, -WarmLat), new Vector2(0, Size));
		float a2 = Mathf.Sqrt(h2 * (2 * Size - h2));

		m_topWarmGizmo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
		m_topWarmGizmo.GetComponent<MeshRenderer>().sharedMaterial = Materials.Warm;
		m_topWarmGizmo.transform.localScale = new Vector3(a * 2 + 0.65f, 0.04f, a * 2 + 0.65f);
		m_topWarmGizmo.transform.position = new Vector3(0, WarmLat, 0);

		m_botWarmGizmo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
		m_botWarmGizmo.GetComponent<MeshRenderer>().sharedMaterial = Materials.Warm;
		m_botWarmGizmo.transform.localScale = new Vector3(a2 * 2 + 0.65f, 0.04f, a2 * 2 + 0.65f);
		m_botWarmGizmo.transform.position = new Vector3(0, -WarmLat, 0);
		m_seconds = 3f;
	}
	public void SetMaterialToBlank()
	{
		foreach (var plate in m_tectonicPlates)
		{
			foreach (var tile in plate.tiles)
			{
				tile.GetComponent<MeshRenderer>().sharedMaterial = Materials.Blank;
			}
		}
	}
	public void SetMaterialToTemp()
	{
		m_layer = 1;
		foreach (var plate in m_tectonicPlates)
		{
			foreach (var tile in plate.tiles)
			{
				if (tile.temperature > 0.0f && tile.temperature <= 1f) tile.GetComponent<MeshRenderer>().sharedMaterial = Materials.Temperature1;
				else if (tile.temperature > 1f && tile.temperature <= 2f) tile.GetComponent<MeshRenderer>().sharedMaterial = Materials.Temperature2;
				else if (tile.temperature > 2f && tile.temperature <= 3f) tile.GetComponent<MeshRenderer>().sharedMaterial = Materials.Temperature3;
				else if (tile.temperature > 3f) tile.GetComponent<MeshRenderer>().sharedMaterial = Materials.Temperature4;
			}
		}
	}
	public void SetMaterialToHum()
	{
		m_layer = 2;
		foreach (var plate in m_tectonicPlates)
		{
			foreach (var tile in plate.tiles)
			{
				if (tile.humidity > -2.0f && tile.humidity <= 1f) tile.GetComponent<MeshRenderer>().sharedMaterial = Materials.Humidity1;
				else if (tile.humidity > 1f && tile.humidity <= 2f) tile.GetComponent<MeshRenderer>().sharedMaterial = Materials.Humidity2;
				else if (tile.humidity > 2f && tile.humidity <= 3f) tile.GetComponent<MeshRenderer>().sharedMaterial = Materials.Humidity3;
				else if (tile.humidity > 3f && tile.humidity <= 4f) tile.GetComponent<MeshRenderer>().sharedMaterial = Materials.Humidity4;
			}
		}
	}
	public void SetMaterialToHeight()
	{
		m_layer = 3;
		foreach (var plate in m_tectonicPlates)
		{
			foreach (var tile in plate.tiles)
			{
				if (tile.altitude > 0.0f) tile.GetComponent<MeshRenderer>().sharedMaterial = Materials.Altitude1;
				if (tile.altitude > 0.02f) tile.GetComponent<MeshRenderer>().sharedMaterial = Materials.Altitude2;
				if (tile.altitude > 0.04f) tile.GetComponent<MeshRenderer>().sharedMaterial = Materials.Altitude3;
			}
		}
	}
	public void ToggleOcean()
	{
		m_oceanActive = !m_oceanActive;
		m_ocean.SetActive(m_oceanActive);
	}
	public void Export()
	{
		GameObject generator = gameObject;

		Combiner.CombineMeshes(generator);
		ObjExporter.MeshesToFile(generator.GetComponents<MeshFilter>(), Application.dataPath, "Planet");
	}
	public void DetermineHeightBiomes()
	{
		m_layer = 0;
		foreach (var plate in m_tectonicPlates)
		{
			foreach (var tile in plate.tiles)
			{
				if (tile.altitude > 0.02f) { tile.GetComponent<MeshRenderer>().sharedMaterial = Materials.Hill; }
				if (tile.altitude > 0.04f) { tile.GetComponent<MeshRenderer>().sharedMaterial = Materials.Mountain; }
				if (tile.altitude <= 0.00f) { tile.GetComponent<MeshRenderer>().sharedMaterial = Materials.Sand1; }
				if (tile.altitude < -0.02f) tile.GetComponent<MeshRenderer>().sharedMaterial = Materials.Sand2;
				if (tile.altitude < -0.04f) tile.GetComponent<MeshRenderer>().sharedMaterial = Materials.Sand3;
				if (tile.altitude < -0.06f) tile.GetComponent<MeshRenderer>().sharedMaterial = Materials.Sand4;
				if (tile.altitude < -0.14f) tile.GetComponent<MeshRenderer>().sharedMaterial = Materials.Lava;
				if (Mathf.Approximately(tile.altitude, 0.02f))
				{
					if (tile.biome == Biome.Sand) tile.GetComponent<MeshRenderer>().sharedMaterial = Materials.Sand1;
					else if (tile.biome == Biome.Glacier) tile.GetComponent<MeshRenderer>().sharedMaterial = Materials.Glacier;
					else if (tile.biome == Biome.Plains) tile.GetComponent<MeshRenderer>().sharedMaterial = Materials.Plains;
					else if (tile.biome == Biome.Snow) tile.GetComponent<MeshRenderer>().sharedMaterial = Materials.Snow;
					else if (tile.biome == Biome.Jungle) tile.GetComponent<MeshRenderer>().sharedMaterial = Materials.Jungle;
					else if (tile.biome == Biome.Desert) tile.GetComponent<MeshRenderer>().sharedMaterial = Materials.Desert;
					else if (tile.biome == Biome.Dirt) tile.GetComponent<MeshRenderer>().sharedMaterial = Materials.Dirt;
					else if (tile.biome == Biome.Tundra) tile.GetComponent<MeshRenderer>().sharedMaterial = Materials.Tundra;
					else if (tile.biome == Biome.Forest) tile.GetComponent<MeshRenderer>().sharedMaterial = Materials.Forest;
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
		if (m_layer == 0) DetermineHeightBiomes();
		else if (m_layer == 1) SetMaterialToTemp();
		else if (m_layer == 2) SetMaterialToHum();
		else if (m_layer == 3) SetMaterialToHeight();
	}

	private void ResetWorld()
	{
		foreach (Transform child in transform)
		{
			Destroy(child.gameObject);
		}
		m_tilesField.text = NumberOfPoints.ToString();
		m_platesField.text = Plates.ToString();
		m_seedField.text = Seed.ToString();
		Random.InitState(Seed);
	}
	private List<Vector3> GeneratePointsUniformly()
	{
		var size = Size;
		var firstPoint = Random.onUnitSphere * size;
		var allVertices = new List<Vector3>() { firstPoint };

		NonRecursiveGenerateBestCandidates(allVertices, size);
		allVertices.Sort((v1, v2) => v1.y.CompareTo(v2.y));

		return allVertices;
	}
	private Vertex3[] GenerateVoronoiVertices(List<Vector3> list)
	{
		var voronoiVertices = new Vertex3[NumberOfPoints + 1]; // VORONOI VERTICES NEED ONE EXTRA ONE IN CENTER
		var i = 0;
		while (i < NumberOfPoints)
		{
			voronoiVertices[i] = list[i].ToVertex3();
			i++;
		}
		voronoiVertices[NumberOfPoints] = new Vertex3(0, 0, 0);

		return voronoiVertices;
	}
	private List<Vector3> GenerateVoronoiHullVertices(VoronoiMesh<Vertex3, Cell3, VoronoiEdge<Vertex3, Cell3>> voronoiMesh)
	{
		var voronoiHullVertices = new List<Vector3>();
		var index = 0;
		foreach (var edge in voronoiMesh.Edges)
		{
			var source = new Vector3(edge.Source.Circumcenter.x, edge.Source.Circumcenter.y, edge.Source.Circumcenter.z);
			var target = new Vector3(edge.Target.Circumcenter.x, edge.Target.Circumcenter.y, edge.Target.Circumcenter.z);
			source *= Size * .4f;
			target *= Size * .4f;
			if (!voronoiHullVertices.Contains(source))
			{
				voronoiHullVertices.Add(source);
			}
			if (!voronoiHullVertices.Contains(target))
			{
				voronoiHullVertices.Add(target);
			}
			index++;
		}

		return voronoiHullVertices;
	}
	private List<int> GenerateConvexHullIndices(List<Face3> faces, List<Vertex3> vertices)
	{
		var indices = new List<int>();
		foreach (var face in faces)
		{
			indices.Add(vertices.IndexOf(face.Vertices[0]));
			indices.Add(vertices.IndexOf(face.Vertices[1]));
			indices.Add(vertices.IndexOf(face.Vertices[2]));
		}

		return indices;
	}
	private Vertex3[] GenerateDelaunayVertices(List<Vector3> voronoiHullVertices)
	{
		var verticesDelaunay = new Vertex3[voronoiHullVertices.Count];
		for (int i = 0; i < verticesDelaunay.Length; i++)
		{
			verticesDelaunay[i] = voronoiHullVertices[i].ToVertex3();
		}

		return verticesDelaunay;
	}
	private Dictionary<Vector3, List<Vector3>> GenerateNormals(List<Vertex3> convexHullVertices, List<int> convexHullIndices)
	{
		var normals = new Dictionary<Vector3, List<Vector3>>();
		for (var j = 0; j < convexHullIndices.Count; j += 3)
		{
			var v0 = convexHullIndices[j + 0];
			var v1 = convexHullIndices[j + 1];
			var v2 = convexHullIndices[j + 2];

			var a = convexHullVertices[v0].ToVector3();
			var b = convexHullVertices[v1].ToVector3();
			var c = convexHullVertices[v2].ToVector3();

			var normal = Vector3.Cross(a - b, a - c);

			// DECLARE KEY AND ROUND IT TO AVOID FLOATING POINT ISSUES
			var key = normal.normalized;
			var roundX = Mathf.Round(key.x * 100) / 100;
			var roundY = Mathf.Round(key.y * 100) / 100;
			var roundZ = Mathf.Round(key.z * 100) / 100;
			var roundedKey = new Vector3(roundX, roundY, roundZ);

			// POPULATE DICTIONARY
			if (!normals.ContainsKey(roundedKey))
			{
				normals.Add(roundedKey, new List<Vector3>());
			}
			normals[roundedKey].Add(a);
			normals[roundedKey].Add(b);
			normals[roundedKey].Add(c);
		}

		return normals;
	}
	private List<VoronoiTile> CreateTiles(Dictionary<Vector3, List<Vector3>> normals)
	{
		var tiles = new List<VoronoiTile>();
		foreach (var pair in normals)
		{
			var tileVerts = new List<Vector3>();
			for (var p = 0; p < pair.Value.Count; ++p)
			{
				tileVerts.Add(pair.Value[p]);
			}
			var tile = new GameObject("Tile", typeof(VoronoiTile), typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
			var thisTile = tile.GetComponent<VoronoiTile>();

			thisTile.Initialize(tileVerts, false); // OPTIMIZE HERE

			tile.GetComponent<MeshFilter>().mesh = thisTile.tileMesh;
			//tile.GetComponent<MeshCollider>().sharedMesh = thisTile.tileMesh;

			thisTile.Normal = pair.Key;
			tiles.Add(thisTile);
			++m_numberOfTiles;
		}

		return tiles;
	}
	private void CreateWaterTiles(Dictionary<Vector3, List<Vector3>> normals)
	{
		m_ocean = new GameObject("Ocean");
		m_ocean.transform.parent = transform;

		var waterTiles = new List<VoronoiTile>();
		foreach (var pair in normals)
		{
			var tileVerts = new List<Vector3>();
			for (var p = 0; p < pair.Value.Count; ++p)
			{
				tileVerts.Add(pair.Value[p]);
			}
			var tile = new GameObject("WaterTile", typeof(VoronoiTile), typeof(MeshFilter), typeof(MeshRenderer));
			var thisTile = tile.GetComponent<VoronoiTile>();
			thisTile.Initialize(tileVerts, true);
			tile.GetComponent<MeshFilter>().mesh = thisTile.tileMesh;
			tile.GetComponent<MeshRenderer>().sharedMaterial = Materials.Ocean;
			tile.transform.parent = m_ocean.transform;
			waterTiles.Add(thisTile);
		}
	}
	private void FindTileNeighbours(List<VoronoiTile> tiles)
	{
		foreach (var tile in tiles)
		{
			tile.FindNeighbors(tiles);
		}
	}
	private List<VoronoiTile> GeneratePlateStartNodes(int count, List<VoronoiTile> allTiles)
	{
		var firstTile = allTiles[Random.Range(0, allTiles.Count)];
		var nodes = new List<VoronoiTile> { firstTile };
		GenerateBestTileCandidates(nodes, count, allTiles);

		return nodes;
	}
	private void GenerateBestTileCandidates(List<VoronoiTile> samples, int depth, List<VoronoiTile> allTiles)
	{
		if (depth == 1)
		{
			return;
		}

		var candidates = new List<VoronoiTile>();
		for (var i = 0; i < NumberOfCandidates; ++i)
		{
			var tile = allTiles[Random.Range(0, allTiles.Count)];
			candidates.Add(tile);
		}
		var bestCandidate = candidates.First();
		var isFirst = true;
		var largestDistance = 0f;
		foreach (var candidate in candidates)
		{
			var closest = FindClosestTile(candidate, samples);
			var distance = Vector3.Distance(closest.centerPoint, candidate.centerPoint);
			if (isFirst || distance > largestDistance)
			{
				largestDistance = distance;
				bestCandidate = candidate;
				isFirst = false;
			}
		}
		samples.Add(bestCandidate);
		GenerateBestTileCandidates(samples, depth - 1, allTiles);
	}
	private List<Material> GenerateMaterials(int amount)
	{
		var materials = new List<Material>();
		for (var i = 0; i < amount; i++)
		{
			var material = new Material(Shader.Find("Standard"));
			material.SetFloat("_Glossiness", 0.0f);
			material.color = new Color(Random.value, Random.value, Random.value, 1.0f);
		}

		for (var j = 0; j < amount; ++j)
		{
			materials.Add(Resources.Load<Material>("" + j));
		}

		return materials;
	}
	private void FloodFillSimultaneous(List<VoronoiTile> plateStartNodes, List<Material> replacements, List<VoronoiTile> allTiles)
	{
		var colors = new List<List<VoronoiTile>>();
		for (var i = 0; i < Plates; ++i)
		{
			colors.Add(new List<VoronoiTile>() { plateStartNodes[i] });
		}

		while (!AreAllProcessed(allTiles))
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
	private void GroupPlates(List<VoronoiTile> tiles)
	{
		m_tectonicPlates = new List<TectonicPlate>();
		for (var i = 0; i < Plates; ++i)
		{
			var plateTest = new GameObject("Plate" + i, typeof(TectonicPlate));
			var testPlateTiles = new List<VoronoiTile>();
			foreach (var voronoiTile in tiles)
			{
				if (voronoiTile.plate == i)
				{
					testPlateTiles.Add(voronoiTile);
				}
			}
			var thisTecPlate = plateTest.GetComponent<TectonicPlate>();
			thisTecPlate.Initialize(ref testPlateTiles);
			if (Random.Range(0, 10) < LandAmount)
			{
				thisTecPlate.isLand = true;
			}
			plateTest.transform.parent = transform;
			m_tectonicPlates.Add(thisTecPlate);
		}
	}
	private void UnifyMesh(Mesh mesh, List<Material> mats, Transform t)
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
		m_waterVerts = new List<Vector3>();
		m_landVerts = new List<Vector3>();
		foreach (var tectonicPlate in m_tectonicPlates)
		{
			if (!tectonicPlate.isLand)
			{
				foreach (var voronoiTile in tectonicPlate.tiles)
				{
					foreach (var vertex in voronoiTile.tileMesh.vertices)
					{
						m_waterVerts.Add(vertex);
					}
				}
			}
			else
			{
				foreach (var voronoiTile in tectonicPlate.tiles)
				{
					foreach (var vertex in voronoiTile.tileMesh.vertices)
					{
						m_landVerts.Add(vertex);
					}
				}
			}
		}
		m_waterVerts = m_waterVerts.Distinct().ToList();
		m_landVerts = m_landVerts.Distinct().ToList();
	}
	private void AssignPlateProperties()
	{
		foreach (var tectonicPlate in m_tectonicPlates)
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
		foreach (var tectonicPlate in m_tectonicPlates)
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
		foreach (var tectonicPlate in m_tectonicPlates)
		{
			foreach (var tile in tectonicPlate.tiles)
			{
				tile.DetermineBiome();
				if (alsoWater) tile.DetermineBaseBiome();
				if (tile.biome == Biome.Sand) tile.GetComponent<MeshRenderer>().sharedMaterial = Materials.Sand1;
				else if (tile.biome == Biome.Glacier) tile.GetComponent<MeshRenderer>().sharedMaterial = Materials.Glacier;
				else if (tile.biome == Biome.Plains) tile.GetComponent<MeshRenderer>().sharedMaterial = Materials.Plains;
				else if (tile.biome == Biome.Snow) tile.GetComponent<MeshRenderer>().sharedMaterial = Materials.Snow;
				else if (tile.biome == Biome.Jungle) tile.GetComponent<MeshRenderer>().sharedMaterial = Materials.Jungle;
				else if (tile.biome == Biome.Desert) tile.GetComponent<MeshRenderer>().sharedMaterial = Materials.Desert;
				else if (tile.biome == Biome.Dirt) tile.GetComponent<MeshRenderer>().sharedMaterial = Materials.Dirt;
				else if (tile.biome == Biome.Tundra) tile.GetComponent<MeshRenderer>().sharedMaterial = Materials.Tundra;
				else if (tile.biome == Biome.Forest) tile.GetComponent<MeshRenderer>().sharedMaterial = Materials.Forest;
				else tile.GetComponent<MeshRenderer>().sharedMaterial = Materials.Blank;
			}
		}
	}
	private void GenerateHeight()
	{
		foreach (var tectonicPlate in m_tectonicPlates)
		{
			if (tectonicPlate.isLand)
			{
				tectonicPlate.PushOutLand(0.02f);
				foreach (var tile in tectonicPlate.tiles)
				{
					float distance = Vector3.Distance(tile.centerPoint, FindClosest(tile.centerPoint, m_waterVerts));
					if (distance > 1.3f)
					{
						tile.Push(0.02f);
						if (distance > 2)
						{
							tile.Push(0.02f);
						}
					}

					//tile.GetComponent<MeshCollider>().sharedMesh = tile.tileMesh;
					//tile.GetComponent<MeshCollider>().convex = true;
				}
			}
			else
			{
				tectonicPlate.PushOutLand(-0.02f);
				foreach (var tile in tectonicPlate.tiles)
				{
					float distance = Vector3.Distance(tile.centerPoint, FindClosest(tile.centerPoint, m_landVerts));
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
					//tile.GetComponent<MeshCollider>().sharedMesh = tile.tileMesh;
					//tile.GetComponent<MeshCollider>().convex = true;
				}
			}
		}
	}
	private void GenerateBestCandidates(List<Vector3> samples, float s, int depth)
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
		GenerateBestCandidates(samples, s, depth - 1);
	}
	private void NonRecursiveGenerateBestCandidates(List<Vector3> samples, float s)
	{
		while (samples.Count < NumberOfPoints)
		{
			var candidates = new List<Vector3>();
			for (var i = 0; i < NumberOfCandidates; ++i)
			{
				var point = Random.onUnitSphere;
				point *= s;
				candidates.Add(point);
			}

			var bestCandidate = Vector3.zero;
			var isFirst = true;
			var largestDistance = 0f;
			foreach (var candidate in candidates)
			{
				var closest = FindClosest(candidate, samples);
				var distance = Vector3.Distance(closest, candidate);

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
	private void FillNeighbors(VoronoiTile node, Material replacement, int plateNumber)
	{
		if (!node.processed)
		{
			node.GetComponent<MeshRenderer>().sharedMaterial = replacement;
			node.plate = plateNumber;
			node.processed = true;
		}

		foreach (var neighbor in node.neighbors)
		{
			if (!neighbor.processed)
			{
				neighbor.GetComponent<MeshRenderer>().sharedMaterial = replacement;
				neighbor.plate = plateNumber;
				neighbor.processed = true;
			}
		}
	}
	private bool AreAllProcessed(List<VoronoiTile> allTiles)
	{
		foreach (var tile in allTiles)
		{
			if (!tile.processed)
			{
				return false;
			}
		}

		return true;
	}
	private Vector3 FindClosest(Vector3 point, List<Vector3> samples)
	{
		var closest = new Vector3();
		var isFirst = true;
		var smallestDistance = 0f;
		foreach (var p in samples)
		{
			var distance = Vector3.Distance(p, point);
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
		var closestTwo = new List<Vector3>();
		var isFirst = true;
		var smallestDistance = 0f;
		foreach (var pos in firstSet)
		{
			var other = FindClosest(pos, secondSet);
			var distance = Vector3.Distance(pos, other);
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
	private VoronoiTile FindClosestTile(VoronoiTile tile, List<VoronoiTile> samples)
	{
		var closest = tile;
		var isFirst = true;
		var smallestDistance = 0f;

		foreach (var sample in samples)
		{
			var distance = Vector3.Distance(sample.centerPoint, tile.centerPoint);
			if (isFirst || distance < smallestDistance)
			{
				smallestDistance = distance;
				closest = sample;
				isFirst = false;
			}
		}

		return closest;
	}
	private Mesh GenerateTriangle(Vector3 a, Vector3 b, Vector3 c)
	{
		var tri = new Mesh
		{
			vertices = new Vector3[] { a, b, c },
			triangles = new int[] { 0, 1, 2 }
		};
		tri.RecalculateNormals();
		tri.RecalculateBounds();

		return tri;
	}


	//private GameObject topColdGizmo;
	//private GameObject botColdGizmo;

	//private GameObject topTempGizmo;
	//private GameObject botTempGizmo;

	//private GameObject topWarmGizmo;
	//private GameObject botWarmGizmo;

	//private float seconds = 4f;

	////public float test { get; set; }
	//// PUBLIC DECLARATIONS
	//public Material planetMat;
	////[Range(200, 3000)]
	//private int numberOfVertices;
	//public int NumberOfVertices
	//{
	//    get { return numberOfVertices;}
	//    set { numberOfVertices = Convert.ToInt32(value); }

	//}
	//private double size = 5;
	//private int state = 0;

	//// MICONVEXHULL DATA STRUCTURES
	//List<Vertex3> convexHullVertices;
	//List<Face3> convexHullFaces;
	//List<int> convexHullIndices;

	//// VERTEX LISTS FOR VORONOI HULL
	//private List<Vector3> allVerts;
	//private List<Vector3> vertsForVoronoiHull;

	//// VORONOI
	//VoronoiMesh<Vertex3, Cell3, VoronoiEdge<Vertex3, Cell3>> voronoiMesh;

	////[SerializeField]
	//private int numberOfTiles = 0;

	//private int seed;
	//public int Seed
	//{
	//    get { return seed; }
	//    set { seed = Convert.ToInt32(value); } 
	//}

	////[Range(10, 200)]
	//public int plates { get; set; }

	////[Range(0.0f, 30.0f)]
	//public float rotationSpeed { get; set; }

	////[Range(0.0f, 5.0f)]
	//public float coldLat { get; set; }
	////[Range(0.0f, 5.0f)]
	//public float tempLat { get; set; }
	////[Range(0.0f, 5.0f)]
	//public float warmLat { get; set; }

	////[Range(-2.0f, 2.0f)]
	//public float humidityModifier { get; set; }

	//public float landAmount { get; set; }

	//private List<Vector3> waterVerts;
	//private List<Vector3> landVerts;
	//private List<TectonicPlate> plateList;

	//private Material sandMat;
	//private Material sand2Mat;
	//private Material sand3Mat;
	//private Material sand4Mat;
	//private Material plainMat;
	//private Material hillMat;
	//private Material snowMat;
	//private Material dirtMat;
	//private Material forestMat;
	//private Material lakeMat;
	//private Material desertMat;
	//private Material mountainMat;
	//private Material tundraMat;
	//private Material jungleMat;
	//private Material glacierMat;
	//private Material swampMat;
	//private Material tempMat;

	//private Material warmMat;
	//private Material temperateMat;
	//private Material coldMat;

	//private Material lavaMat;

	//private Material temp0Mat;
	//private Material temp1Mat;
	//private Material temp2Mat;
	//private Material temp3Mat;

	//private Material hum0Mat;
	//private Material hum1Mat;
	//private Material hum2Mat;
	//private Material hum3Mat;

	//private Material h0Mat;
	//private Material h1Mat;
	//private Material h2Mat;

	//public static Text[] texts;
	//public static Text biomeText;
	//public static Text actionText;
	//public static Text toolText;
	//private GameObject ocean;

	//public int layer = 0;
	//private bool oceanActive = true;

	//void Start()
	//{
	//    numberOfVertices = 200;
	//    plates = 20;
	//    rotationSpeed = 4;
	//    humidityModifier = 0;
	//    coldLat = 4.2f;
	//    tempLat = 2.7f;
	//    warmLat = 0.9f;
	//    seed = Random.Range(Int32.MinValue, Int32.MaxValue);
	//    landAmount = 4f;

	//    texts = FindObjectsOfType<Text>();
	//    biomeText = texts[12];

	//    Create();
	//}

	//public void Export()
	//{
	//    GameObject generator = this.gameObject;

	//    Combiner.CombineMeshes(generator);
	//    ObjExporter.MeshesToFile(generator.GetComponents<MeshFilter>(), Application.dataPath, "Planet");
	//}

	//private void UnifyMesh(ref Mesh mesh, ref List<Material> mats, Transform t)
	//{
	//    // IT ONLY TAKES ONE MESH FILTER PER PLATE
	//    MeshFilter[] meshFilters = t.GetComponentsInChildren<MeshFilter>();
	//    CombineInstance[] combine = new CombineInstance[meshFilters.Length];
	//    var index = 0;

	//    for (int i = 0; i < meshFilters.Length; ++i)
	//    {
	//        if (meshFilters[i].sharedMesh == null) continue;
	//        combine[index].mesh = meshFilters[i].sharedMesh;
	//        //Debug.Log(meshFilters[i].mesh.subMeshCount);
	//        combine[index++].transform = meshFilters[i].transform.localToWorldMatrix;
	//        Material mat = meshFilters[i].gameObject.GetComponent<MeshRenderer>().sharedMaterial;
	//        //if(!mats.Contains(mat)) mats.Add(mat);
	//        mats.Add(mat);
	//    }
	//    mesh.CombineMeshes(combine, false);
	//}

	//public void Create()
	//{
	//    foreach (Transform child in transform)
	//    {
	//        Destroy(child.gameObject);
	//    }

	//    InputField[] fields = FindObjectsOfType<InputField>();
	//    InputField numseed = fields[0];
	//    InputField numplates = fields[1];
	//    InputField numtiles = fields[2];
	//    numberOfVertices = Convert.ToInt32(numtiles.text);
	//    plates = Convert.ToInt32(numplates.text);
	//    seed = Convert.ToInt32(numseed.text);

	//    // INITIALIZATION
	//    Vertex3[] vertices = new Vertex3[numberOfVertices];
	//    Vector3[] meshVerts = new Vector3[numberOfVertices];
	//    allVerts = new List<Vector3>();
	//    vertsForVoronoiHull = new List<Vector3>();

	//    // VORONOI VERTICES NEED ONE EXTRA ONE IN CENTER
	//    Vertex3[] voronoiVertices = new Vertex3[numberOfVertices + 1];

	//    // RANDOM SEED
	//    Random.seed = seed;

	//    // GENERATE UNIFORM POINTS
	//    allVerts = GeneratePointsUniformly(); 
	//    allVerts.Sort((v1, v2) => v1.y.CompareTo(v2.y));

	//    // SET INDICES FOR VORONOI
	//    int i = 0;
	//    while (i < numberOfVertices)
	//    {
	//        vertices[i] = new Vertex3(allVerts[i].x, allVerts[i].y, allVerts[i].z);
	//        voronoiVertices[i] = vertices[i];
	//        meshVerts[i] = vertices[i].ToVector3(); ;
	//        i++;
	//    }
	//    // SET LAST EXTRA VERTEX
	//    voronoiVertices[numberOfVertices] = new Vertex3(0, 0, 0);


	//    // VORONOI
	//    voronoiMesh = VoronoiMesh.Create<Vertex3, Cell3>(voronoiVertices);



	//    // VORONOI HULL GENERATION
	//    int index = 0;
	//    foreach (var edge in voronoiMesh.Edges)
	//    {
	//        Vector3 source = new Vector3(edge.Source.Circumcenter.x, edge.Source.Circumcenter.y, edge.Source.Circumcenter.z);
	//        Vector3 target = new Vector3(edge.Target.Circumcenter.x, edge.Target.Circumcenter.y, edge.Target.Circumcenter.z);
	//        source *= ((float)size / 2.5f);
	//        target *= ((float)size / 2.5f);
	//        vertsForVoronoiHull.Add(source);
	//        vertsForVoronoiHull.Add(target);
	//        index++;
	//    }

	//    // REMOVE DUPLICATE POINTS
	//    vertsForVoronoiHull = vertsForVoronoiHull.Distinct().ToList();

	//    // CONVERT FROM VECTOR3 LIST TO VERTEX3 LIST FOR FINAL HULL
	//    Vertex3[] verticesDelaunay = new Vertex3[vertsForVoronoiHull.Count];

	//    int g = 0;
	//    while (g < vertsForVoronoiHull.Count)
	//    {
	//        verticesDelaunay[g] = new Vertex3(vertsForVoronoiHull[g].x, vertsForVoronoiHull[g].y, vertsForVoronoiHull[g].z);
	//        g++;
	//    }

	//    // GENERATE VORONOI HULL
	//    ConvexHull<Vertex3, Face3> convexHull = ConvexHull.Create<Vertex3, Face3>(verticesDelaunay);
	//    convexHullVertices = new List<Vertex3>(convexHull.Points);
	//    convexHullFaces = new List<Face3>(convexHull.Faces);
	//    convexHullIndices = new List<int>();

	//    foreach (Face3 f in convexHullFaces)
	//    {
	//        convexHullIndices.Add(convexHullVertices.IndexOf(f.Vertices[0]));
	//        convexHullIndices.Add(convexHullVertices.IndexOf(f.Vertices[1]));
	//        convexHullIndices.Add(convexHullVertices.IndexOf(f.Vertices[2]));
	//    }

	//    Dictionary<Vector3, List<Vector3>> normals = new Dictionary<Vector3, List<Vector3>>();

	//    // CREATE TRIANGLES FOR MESH
	//    for (int j = 0; j < convexHullIndices.Count; j += 3)
	//    {
	//        int v0 = convexHullIndices[j + 0];
	//        int v1 = convexHullIndices[j + 1];
	//        int v2 = convexHullIndices[j + 2];

	//        Vector3 a = new Vector3((float)convexHullVertices[v0].x, (float)convexHullVertices[v0].y, (float)convexHullVertices[v0].z);
	//        Vector3 b = new Vector3((float)convexHullVertices[v1].x, (float)convexHullVertices[v1].y, (float)convexHullVertices[v1].z);
	//        Vector3 c = new Vector3((float)convexHullVertices[v2].x, (float)convexHullVertices[v2].y, (float)convexHullVertices[v2].z);

	//        Vector3 normal = Vector3.Cross(a - b, a - c);

	//        // DECLARE KEY AND ROUND IT TO AVOID FLOATING POINT ISSUES
	//        Vector3 key = normal.normalized;
	//        float roundX = Mathf.Round(key.x * 100) / 100;
	//        float roundY = Mathf.Round(key.y * 100) / 100;
	//        float roundZ = Mathf.Round(key.z * 100) / 100;
	//        Vector3 roundedKey = new Vector3(roundX, roundY, roundZ);

	//        // POPULATE DICTIONARY
	//        if (!normals.ContainsKey(roundedKey))
	//        {
	//            normals.Add(roundedKey, new List<Vector3>());
	//        }
	//        normals[roundedKey].Add(a);
	//        normals[roundedKey].Add(b);
	//        normals[roundedKey].Add(c);
	//    }

	//    // CREATE VORONOI TILES
	//    List<VoronoiTile> tiles = new List<VoronoiTile>();
	//    foreach (var pair in normals)
	//    {
	//        List<Vector3> tileVerts = new List<Vector3>();
	//        for (int p = 0; p < pair.Value.Count; ++p)
	//        {
	//            tileVerts.Add(pair.Value[p]);
	//        }
	//        GameObject tile = new GameObject("Tile", typeof(VoronoiTile), typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
	//        var thisTile = tile.GetComponent<VoronoiTile>() as VoronoiTile;

	//        thisTile.Initialize(tileVerts, false); // OPTIMIZE HERE

	//        tile.GetComponent<MeshFilter>().mesh = thisTile.tileMesh;
	//        tile.GetComponent<MeshCollider>().sharedMesh = thisTile.tileMesh;

	//        thisTile.Normal = pair.Key;
	//        tiles.Add(thisTile);
	//        ++numberOfTiles;
	//    }

	//    foreach (var tile in tiles)
	//    {
	//        tile.FindNeighbors(tiles); 
	//    }

	//    List<VoronoiTile> waterTiles = new List<VoronoiTile>();
	//    Material oceanMat = (Material)Resources.Load("Ocean", typeof(Material));
	//    foreach (var pair in normals)
	//    {
	//        List<Vector3> tileVerts = new List<Vector3>();
	//        for (int p = 0; p < pair.Value.Count; ++p)
	//        {
	//            tileVerts.Add(pair.Value[p]);
	//        }
	//        GameObject tile = new GameObject("WaterTile", typeof(VoronoiTile), typeof(MeshFilter), typeof(MeshRenderer));
	//        var thisTile = tile.GetComponent<VoronoiTile>() as VoronoiTile;
	//        thisTile.Initialize(tileVerts, true);
	//        tile.GetComponent<MeshFilter>().mesh = thisTile.tileMesh;
	//        tile.GetComponent<MeshRenderer>().sharedMaterial = oceanMat;
	//        waterTiles.Add(thisTile);
	//    }

	//    ocean = new GameObject("Ocean");
	//    ocean.transform.parent = transform;
	//    foreach (var tile in waterTiles)
	//    {
	//        tile.transform.parent = ocean.transform;
	//    }

	//    // FLOOD FILLS
	//    // GENERATE PLATES
	//    List<VoronoiTile> plateStartNodes = GeneratePlateStartNodes(plates, ref tiles);

	//    // GENERATE PLATE MATERIALS
	//    List<Material> plateMaterials = GenerateMaterials(1); // changed here

	//    // GENERATE START LISTS
	//    List<List<VoronoiTile>> colors = new List<List<VoronoiTile>>();
	//    for (int b = 0; b < plates; ++b)
	//    {
	//        colors.Add(new List<VoronoiTile>() { plateStartNodes[b] });
	//    }


	//    // FILL
	//    FloodFillSimultaneous(ref colors, plateMaterials, ref tiles);


	//    // GROUP PLATES
	//    plateList = new List<TectonicPlate>();
	//    for (int q = 0; q < plates; ++q)
	//    {
	//        GameObject plateTest = new GameObject("Plate" + q, typeof(TectonicPlate));
	//        List<VoronoiTile> testPlateTiles = new List<VoronoiTile>();
	//        foreach (var voronoiTile in tiles)
	//        {
	//            if (voronoiTile.plate == q) testPlateTiles.Add(voronoiTile);
	//        }
	//        var thisTecPlate = plateTest.GetComponent<TectonicPlate>();
	//        thisTecPlate.Initialize(ref testPlateTiles);
	//        int land = Random.Range(0, 10);
	//        if (land < landAmount) thisTecPlate.isLand = true;
	//        plateTest.transform.parent = transform;
	//        plateList.Add(thisTecPlate);
	//    }

	//    LoadMaterials();

	//    // CREATE WATER AND LAND AREAS FOR HEIGHT
	//    FindWaterAndLandPoints();

	//    // DETERMINE BIOMES
	//    AssignPlateProperties();
	//    AssignTileProperties();
	//    DetermineBiomes(true);
	//    GenerateHeight();
	//    DetermineHeightBiomes();
	//}

	//public void SetMaterialToBlank()
	//{
	//    foreach (var plate in plateList)
	//    {
	//        foreach (var tile in plate.tiles)
	//        {
	//            tile.GetComponent<MeshRenderer>().sharedMaterial = tempMat;
	//        }
	//    }
	//}

	//public void SetMaterialToTemp()
	//{
	//    layer = 1;
	//    foreach (var plate in plateList)
	//    {
	//        foreach (var tile in plate.tiles)
	//        {
	//            if (tile.temperature > 0.0f && tile.temperature <= 1f) tile.GetComponent<MeshRenderer>().sharedMaterial = temp0Mat;
	//            else if (tile.temperature > 1f && tile.temperature <= 2f) tile.GetComponent<MeshRenderer>().sharedMaterial = temp1Mat;
	//            else if (tile.temperature > 2f && tile.temperature <= 3f) tile.GetComponent<MeshRenderer>().sharedMaterial = temp2Mat;
	//            else if (tile.temperature > 3f) tile.GetComponent<MeshRenderer>().sharedMaterial = temp3Mat;
	//        }
	//    }
	//}

	//public void SetMaterialToHum()
	//{
	//    layer = 2;
	//    foreach (var plate in plateList)
	//    {
	//        foreach (var tile in plate.tiles)
	//        {
	//            if (tile.humidity > -2.0f && tile.humidity <= 1f) tile.GetComponent<MeshRenderer>().sharedMaterial = hum0Mat;
	//            else if (tile.humidity >1f && tile.humidity <= 2f) tile.GetComponent<MeshRenderer>().sharedMaterial = hum1Mat;
	//            else if (tile.humidity > 2f && tile.humidity <= 3f) tile.GetComponent<MeshRenderer>().sharedMaterial = hum2Mat;
	//            else if (tile.humidity > 3f && tile.humidity <= 4f) tile.GetComponent<MeshRenderer>().sharedMaterial = hum3Mat;
	//        }
	//    }
	//}

	//public void SetMaterialToHeight()
	//{
	//    layer = 3;
	//    foreach (var plate in plateList)
	//    {
	//        foreach (var tile in plate.tiles)
	//        {
	//            if (tile.altitude > 0.0f ) tile.GetComponent<MeshRenderer>().sharedMaterial = h0Mat;
	//            if (tile.altitude > 0.02f) tile.GetComponent<MeshRenderer>().sharedMaterial = h1Mat;
	//            if (tile.altitude > 0.04f) tile.GetComponent<MeshRenderer>().sharedMaterial = h2Mat;
	//        }
	//    }
	//}

	//public void ToggleOcean()
	//{
	//    oceanActive = !oceanActive;
	//    ocean.SetActive(oceanActive);
	//}

	//private void FindWaterAndLandPoints()
	//{
	//    waterVerts = new List<Vector3>();
	//    landVerts = new List<Vector3>();
	//    foreach (var tectonicPlate in plateList)
	//    {
	//        if (!tectonicPlate.isLand)
	//        {
	//            foreach (var voronoiTile in tectonicPlate.tiles)
	//            {
	//                foreach (var vertex in voronoiTile.tileMesh.vertices)
	//                {
	//                    waterVerts.Add(vertex);
	//                }
	//            }
	//        }
	//        else
	//        {
	//            foreach (var voronoiTile in tectonicPlate.tiles)
	//            {
	//                foreach (var vertex in voronoiTile.tileMesh.vertices)
	//                {
	//                    landVerts.Add(vertex);
	//                }
	//            }
	//        }
	//    }
	//    waterVerts = waterVerts.Distinct().ToList();
	//    landVerts = landVerts.Distinct().ToList();
	//}
	//private void LoadMaterials()
	//{
	//    sandMat = (Material)Resources.Load("Sand", typeof(Material));
	//    sand2Mat = (Material)Resources.Load("Sand2", typeof(Material));
	//    sand3Mat = (Material)Resources.Load("Sand3", typeof(Material));
	//    sand4Mat = (Material)Resources.Load("Sand4", typeof(Material));
	//    plainMat = (Material)Resources.Load("Plains", typeof(Material));
	//    hillMat = (Material)Resources.Load("Hill", typeof(Material));
	//    snowMat = (Material)Resources.Load("Snow", typeof(Material));
	//    dirtMat = (Material)Resources.Load("Dirt", typeof(Material));
	//    forestMat = (Material)Resources.Load("Forest", typeof(Material));
	//    lakeMat = (Material)Resources.Load("Lake", typeof(Material));
	//    desertMat = (Material)Resources.Load("Desert", typeof(Material));
	//    mountainMat = (Material)Resources.Load("Mountains", typeof(Material));
	//    tundraMat = (Material)Resources.Load("Tundra", typeof(Material));
	//    jungleMat = (Material)Resources.Load("Jungle", typeof(Material));
	//    glacierMat = (Material)Resources.Load("Glacier", typeof(Material));
	//    swampMat = (Material)Resources.Load("Swamp", typeof(Material));
	//    tempMat = (Material)Resources.Load("Temp", typeof(Material));

	//    temperateMat = (Material)Resources.Load("Temperate", typeof(Material));
	//    warmMat = (Material)Resources.Load("Warm", typeof(Material));
	//    coldMat = (Material)Resources.Load("Cold", typeof(Material));

	//    lavaMat = (Material)Resources.Load("Lava", typeof(Material));

	//    temp0Mat = (Material)Resources.Load("MTemp0", typeof(Material));
	//    temp1Mat = (Material)Resources.Load("MTemp1", typeof(Material));
	//    temp2Mat = (Material)Resources.Load("MTemp2", typeof(Material));
	//    temp3Mat = (Material)Resources.Load("MTemp3", typeof(Material));

	//    hum0Mat = (Material)Resources.Load("MHum0", typeof(Material));
	//    hum1Mat = (Material)Resources.Load("MHum1", typeof(Material));
	//    hum2Mat = (Material)Resources.Load("MHum2", typeof(Material));
	//    hum3Mat = (Material)Resources.Load("MHum3", typeof(Material));

	//    h0Mat = (Material)Resources.Load("MH0", typeof(Material));
	//    h1Mat = (Material)Resources.Load("MH1", typeof(Material));
	//    h2Mat = (Material)Resources.Load("MH2", typeof(Material));

	//}

	//private void AssignPlateProperties()
	//{
	//    foreach (var tectonicPlate in plateList)
	//    {
	//        float distanceFromEquator = tectonicPlate.middle.y;
	//        if (distanceFromEquator > coldLat || distanceFromEquator < -coldLat) // POLES
	//        {
	//            tectonicPlate.SetTemp(0.5f);
	//            tectonicPlate.SetHumidity(tectonicPlate.baseHumidity);
	//        }
	//        else if (distanceFromEquator < coldLat && distanceFromEquator > tempLat || distanceFromEquator > -coldLat && distanceFromEquator < -tempLat) // COLD
	//        {
	//            tectonicPlate.SetTemp(1.5f);
	//            tectonicPlate.SetHumidity(tectonicPlate.baseHumidity);
	//        }
	//        else if (distanceFromEquator < tempLat && distanceFromEquator > warmLat || distanceFromEquator > -tempLat && distanceFromEquator < -warmLat) // TEMPERED
	//        {
	//            tectonicPlate.SetTemp(2.5f);
	//            tectonicPlate.SetHumidity(tectonicPlate.baseHumidity);
	//        }
	//        else if (distanceFromEquator < warmLat && distanceFromEquator >= 0.0 || distanceFromEquator > -warmLat && distanceFromEquator <= 0.0) // WARM
	//        {
	//            tectonicPlate.SetTemp(3.5f);
	//            tectonicPlate.SetHumidity(tectonicPlate.baseHumidity);
	//        }
	//    }
	//}
	//private void AssignTileProperties()
	//{
	//    foreach (var tectonicPlate in plateList)
	//    {
	//        foreach (var tile in tectonicPlate.tiles)
	//        {
	//            float distanceFromEquator = tile.centerPoint.y;
	//            if (distanceFromEquator > coldLat || distanceFromEquator < -coldLat)
	//            {
	//                tile.temperature = (tile.temperature + 0.5f) / 2;
	//            }
	//            else if (distanceFromEquator < coldLat && distanceFromEquator > tempLat || distanceFromEquator > -coldLat && distanceFromEquator < -tempLat)
	//            {
	//                tile.temperature = (tile.temperature + 1.5f) / 2;
	//            }
	//            else if (distanceFromEquator < tempLat && distanceFromEquator > warmLat || distanceFromEquator > -tempLat && distanceFromEquator < -warmLat)
	//            {
	//                tile.temperature = (tile.temperature + 2.5f) / 2;
	//            }
	//            else if (distanceFromEquator < warmLat && distanceFromEquator > 0.0 || distanceFromEquator > -warmLat && distanceFromEquator < 0.0)
	//            {
	//                tile.temperature = (tile.temperature + 3.5f) / 2;
	//            }
	//            tile.humidity += humidityModifier;
	//        }
	//    }
	//}
	//private void DetermineBiomes(bool alsoWater)
	//{
	//    foreach (var tectonicPlate in plateList)
	//    {
	//        foreach (var tile in tectonicPlate.tiles)
	//        {
	//            tile.DetermineBiome();
	//            if(alsoWater) tile.DetermineBaseBiome();
	//            if (tile.biome == Biome.Sand) tile.GetComponent<MeshRenderer>().sharedMaterial = sandMat;
	//            else if (tile.biome == Biome.Glacier) tile.GetComponent<MeshRenderer>().sharedMaterial = glacierMat;
	//            else if (tile.biome == Biome.Plains) tile.GetComponent<MeshRenderer>().sharedMaterial = plainMat;
	//            else if (tile.biome == Biome.Snow) tile.GetComponent<MeshRenderer>().sharedMaterial = snowMat;
	//            else if (tile.biome == Biome.Jungle) tile.GetComponent<MeshRenderer>().sharedMaterial = jungleMat;
	//            else if (tile.biome == Biome.Desert) tile.GetComponent<MeshRenderer>().sharedMaterial = desertMat;
	//            else if (tile.biome == Biome.Dirt) tile.GetComponent<MeshRenderer>().sharedMaterial = dirtMat;
	//            else if (tile.biome == Biome.Tundra) tile.GetComponent<MeshRenderer>().sharedMaterial = tundraMat;
	//            else if (tile.biome == Biome.Forest) tile.GetComponent<MeshRenderer>().sharedMaterial = forestMat;
	//            else tile.GetComponent<MeshRenderer>().sharedMaterial = tempMat;
	//        }
	//    }
	//}

	//public void DetermineHeightBiomes()
	//{
	//    layer = 0;
	//    foreach (var plate in plateList)
	//    {
	//        foreach (var tile in plate.tiles)
	//        {
	//            if (tile.altitude > 0.02f) { tile.GetComponent<MeshRenderer>().sharedMaterial = hillMat;}
	//            if (tile.altitude > 0.04f) { tile.GetComponent<MeshRenderer>().sharedMaterial = mountainMat; }
	//            if (tile.altitude <= 0.00f) { tile.GetComponent<MeshRenderer>().sharedMaterial = sandMat;}
	//            if (tile.altitude < -0.02f) tile.GetComponent<MeshRenderer>().sharedMaterial = sand2Mat;
	//            if (tile.altitude < -0.04f) tile.GetComponent<MeshRenderer>().sharedMaterial = sand3Mat;
	//            if (tile.altitude < -0.06f) tile.GetComponent<MeshRenderer>().sharedMaterial = sand4Mat;
	//            if (tile.altitude < -0.14f) tile.GetComponent<MeshRenderer>().sharedMaterial = lavaMat;
	//            if (Mathf.Approximately(tile.altitude, 0.02f))
	//            {
	//                if (tile.biome == Biome.Sand) tile.GetComponent<MeshRenderer>().sharedMaterial = sandMat;
	//                else if (tile.biome == Biome.Glacier) tile.GetComponent<MeshRenderer>().sharedMaterial = glacierMat;
	//                else if (tile.biome == Biome.Plains) tile.GetComponent<MeshRenderer>().sharedMaterial = plainMat;
	//                else if (tile.biome == Biome.Snow) tile.GetComponent<MeshRenderer>().sharedMaterial = snowMat;
	//                else if (tile.biome == Biome.Jungle) tile.GetComponent<MeshRenderer>().sharedMaterial = jungleMat;
	//                else if (tile.biome == Biome.Desert) tile.GetComponent<MeshRenderer>().sharedMaterial = desertMat;
	//                else if (tile.biome == Biome.Dirt) tile.GetComponent<MeshRenderer>().sharedMaterial = dirtMat;
	//                else if (tile.biome == Biome.Tundra) tile.GetComponent<MeshRenderer>().sharedMaterial = tundraMat;
	//                else if (tile.biome == Biome.Forest) tile.GetComponent<MeshRenderer>().sharedMaterial = forestMat;
	//            }
	//            tile.GetComponent<MeshCollider>().sharedMesh = tile.tileMesh;
	//            tile.GetComponent<MeshCollider>().convex = true;
	//        }
	//    }
	//}

	//public void LateUpdate()
	//{
	//    if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
	//    {
	//        if (layer == 0) DetermineHeightBiomes();
	//        else if (layer == 1) SetMaterialToTemp();
	//        else if (layer == 2) SetMaterialToHum();
	//    }
	//}

	//private void GenerateHeight()
	//{
	//    foreach (var tectonicPlate in plateList)
	//    {
	//        if (tectonicPlate.isLand)
	//        {
	//            tectonicPlate.PushOutLand(0.02f);
	//            foreach (var tile in tectonicPlate.tiles)
	//            {
	//                float distance = Vector3.Distance(tile.centerPoint, FindClosest(tile.centerPoint, waterVerts));
	//                if (distance > 1.3f)
	//                {
	//                    tile.Push(0.02f);
	//                    if (distance > 2)
	//                    {
	//                        tile.Push(0.02f);
	//                    }
	//                }

	//                tile.GetComponent<MeshCollider>().sharedMesh = tile.tileMesh;
	//                tile.GetComponent<MeshCollider>().convex = true;
	//            }
	//        }
	//        else
	//        {
	//            tectonicPlate.PushOutLand(-0.02f);
	//            foreach (var tile in tectonicPlate.tiles)
	//            {
	//                float distance = Vector3.Distance(tile.centerPoint, FindClosest(tile.centerPoint, landVerts));
	//                if (distance > 1)
	//                {
	//                    tile.Push(-0.02f);
	//                    if (distance > 2)
	//                    {
	//                        tile.Push(-0.02f);
	//                        if (distance > 3)
	//                        {
	//                            tile.Push(-0.02f);
	//                        }
	//                    }
	//                }
	//                tile.GetComponent<MeshCollider>().sharedMesh = tile.tileMesh;
	//                tile.GetComponent<MeshCollider>().convex = true;
	//            }
	//        }
	//    }
	//}

	//public void RecalculateBiomes()
	//{
	//    AssignPlateProperties();
	//    AssignTileProperties();
	//    DetermineBiomes(false);
	//    if(layer == 0) DetermineHeightBiomes();
	//    else if(layer == 1) SetMaterialToTemp();
	//    else if (layer == 2) SetMaterialToHum();
	//    else if (layer == 3) SetMaterialToHeight();
	//}

	//private List<Material> GenerateMaterials(int amount)
	//{
	//    List<Material> materials = new List<Material>();
	//    for (int i = 0; i < amount; i++)
	//    {
	//        var material = new Material(Shader.Find("Standard"));
	//        material.SetFloat("_Glossiness", 0.0f);
	//        material.color = new Color(Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f), 1);
	//        //AssetDatabase.CreateAsset(material, "Assets/Resources/" + i + ".mat");
	//    }
	//    for (int j = 0; j < amount; ++j)
	//    {
	//        materials.Add(Resources.Load("" + j, typeof(Material)) as Material);
	//    }
	//    return materials;
	//}
	//private Mesh GenerateTriangle(Vector3 a, Vector3 b, Vector3 c)
	//{
	//    Mesh tri = new Mesh();
	//    tri.vertices = new Vector3[] {a, b, c};
	//    tri.triangles = new int[] {0, 1, 2};
	//    tri.RecalculateNormals();
	//    tri.RecalculateBounds();
	//    ;
	//    return tri;
	//}
	//private List<Vector3> GeneratePointsUniformly()
	//{
	//    float s = (float)size;
	//    Vector3 firstPoint = Random.onUnitSphere;
	//    firstPoint *= s;
	//    List<Vector3> bestCandidates = new List<Vector3>();
	//    bestCandidates.Add(firstPoint);
	//    NonRecursiveGenerateBestCandidates(ref bestCandidates, s);
	//    return bestCandidates;
	//}
	//private void GenerateBestCandidates(ref List<Vector3> samples, float s, int depth)
	//{
	//    if (depth == 1) return;
	//    List<Vector3> candidates = new List<Vector3>();
	//    for (int i = 0; i < 10; ++i)
	//    {
	//        Vector3 point = Random.onUnitSphere;
	//        point *= s;
	//        candidates.Add(point);
	//    }

	//    Vector3 bestCandidate = Vector3.zero;
	//    bool isFirst = true;
	//    float largestDistance = 0;
	//    foreach (var candidate in candidates)
	//    {
	//        Vector3 closest = FindClosest(candidate, samples);
	//        float distance = Vector3.Distance(closest, candidate);

	//        if (isFirst || distance > largestDistance)
	//        {
	//            largestDistance = distance;
	//            bestCandidate = candidate;
	//            isFirst = false;
	//        }
	//    }
	//    samples.Add(bestCandidate);
	//    GenerateBestCandidates(ref samples, s, depth - 1);
	//}

	//private void NonRecursiveGenerateBestCandidates(ref List<Vector3> samples, float s)
	//{
	//    while (samples.Count < numberOfVertices)
	//    {
	//        List<Vector3> candidates = new List<Vector3>();
	//        for (int i = 0; i < 10; ++i)
	//        {
	//            Vector3 point = Random.onUnitSphere;
	//            point *= s;
	//            candidates.Add(point);
	//        }

	//        Vector3 bestCandidate = Vector3.zero;
	//        bool isFirst = true;
	//        float largestDistance = 0;
	//        foreach (var candidate in candidates)
	//        {
	//            Vector3 closest = FindClosest(candidate, samples);
	//            float distance = Vector3.Distance(closest, candidate);

	//            if (isFirst || distance > largestDistance)
	//            {
	//                largestDistance = distance;
	//                bestCandidate = candidate;
	//                isFirst = false;
	//            }
	//        }
	//        samples.Add(bestCandidate);
	//    }
	//    //DateTime after = DateTime.Now;
	//    //TimeSpan duration = after.Subtract(before);
	//    //Debug.Log("Duration in milliseconds at depth " + depth + ": " + duration.Milliseconds);

	//}
	//private Vector3 FindClosest(Vector3 point, List<Vector3> samples)
	//{
	//    Vector3 closest = new Vector3();
	//    bool isFirst = true;
	//    float smallestDistance = 0;
	//    foreach (var p in samples)
	//    {
	//        float distance = Vector3.Distance(p, point);
	//        if (isFirst || distance < smallestDistance)
	//        {
	//            smallestDistance = distance;
	//            closest = p;
	//            isFirst = false;
	//        }
	//    }
	//    return closest;
	//}
	//private List<Vector3> FindClosestTwo(List<Vector3> firstSet, List<Vector3> secondSet)
	//{
	//    List<Vector3> closestTwo = new List<Vector3>();
	//    bool isFirst = true;
	//    float smallestDistance = 0;
	//    foreach (var pos in firstSet)
	//    {
	//        Vector3 other = FindClosest(pos, secondSet);
	//        float distance = Vector3.Distance(pos, other);
	//        if (isFirst || distance < smallestDistance)
	//        {
	//            closestTwo.Clear();
	//            smallestDistance = distance;
	//            closestTwo.Add(pos);
	//            closestTwo.Add(other);
	//            isFirst = false;
	//        }
	//    }
	//    return closestTwo;
	//}
	//private List<VoronoiTile> GeneratePlateStartNodes(int count, ref List<VoronoiTile> allTiles)
	//{
	//    VoronoiTile firstTile = allTiles[Random.Range(0, allTiles.Count)];
	//    List<VoronoiTile> nodes = new List<VoronoiTile>();
	//    nodes.Add(firstTile);
	//    GenerateBestTileCandidates(ref nodes, count, ref allTiles);
	//    return nodes;
	//}
	//private void GenerateBestTileCandidates(ref List<VoronoiTile> samples, int depth, ref List<VoronoiTile> allTiles)
	//{
	//    if (depth == 1) return;
	//    List<VoronoiTile> candidates = new List<VoronoiTile>();
	//    for (int i = 0; i < 10; ++i)
	//    {
	//        VoronoiTile tile = allTiles[Random.Range(0, allTiles.Count)];
	//        candidates.Add(tile);
	//    }
	//    VoronoiTile bestCandidate = candidates.First();
	//    bool isFirst = true;
	//    float largestDistance = 0;
	//    foreach (var candidate in candidates)
	//    {
	//        VoronoiTile closest = FindClosestTile(candidate, ref samples);
	//        float distance = Vector3.Distance(closest.centerPoint, candidate.centerPoint);
	//        if (isFirst || distance > largestDistance)
	//        {
	//            largestDistance = distance;
	//            bestCandidate = candidate;
	//            isFirst = false;
	//        }
	//    }
	//    samples.Add(bestCandidate);
	//    GenerateBestTileCandidates(ref samples, depth - 1, ref allTiles);
	//}
	//private VoronoiTile FindClosestTile(VoronoiTile tile, ref List<VoronoiTile> samples)
	//{
	//    VoronoiTile closest = tile;
	//    bool isFirst = true;
	//    float smallestDistance = 0;
	//    foreach (var sample in samples)
	//    {
	//        float distance = Vector3.Distance(sample.centerPoint, tile.centerPoint);
	//        if (isFirst || distance < smallestDistance)
	//        {
	//            smallestDistance = distance;
	//            closest = sample;
	//            isFirst = false;
	//        }
	//    }
	//    return closest;
	//}
	//private void FillNeighbors(VoronoiTile node, Material replacement, int plateNumber)
	//{
	//    if (node.processed == false)
	//    {
	//        node.GetComponent<MeshRenderer>().sharedMaterial = replacement;
	//        node.plate = plateNumber;
	//        node.processed = true;
	//    }
	//    foreach (var neighbor in node.neighbors)
	//    {
	//        if (neighbor.processed == false)
	//        {
	//            neighbor.GetComponent<MeshRenderer>().sharedMaterial = replacement;
	//            neighbor.plate = plateNumber;
	//            neighbor.processed = true;
	//        }
	//    }
	//}
	//private void FloodFillSimultaneous(ref List<List<VoronoiTile>> colors, List<Material> replacements, ref List<VoronoiTile> allTiles)
	//{
	//    while (!AreAllProcessed(ref allTiles))
	//    {
	//        for (int colorIndex = 0; colorIndex < colors.Count; ++colorIndex) // for each node in colors, fill neighbors
	//        {
	//            List<VoronoiTile> newNodes = new List<VoronoiTile>();
	//            for (int tileIndex = 0; tileIndex < colors[colorIndex].Count; tileIndex++) // go over each node in the colors (first step only one) and fill neighbors
	//            {
	//                VoronoiTile tile = colors[colorIndex][tileIndex];
	//                FillNeighbors(tile, replacements[0], colorIndex); // changed here
	//                foreach (var neighbor in tile.neighbors)
	//                {
	//                    //neighbor.processed = true;
	//                    foreach (var nbr in neighbor.neighbors)
	//                    {
	//                        if (!nbr.processed) newNodes.Add(nbr);
	//                    }
	//                }
	//            }
	//            colors[colorIndex].Clear();
	//            colors[colorIndex] = newNodes;
	//        }
	//    }
	//}
	//private bool AreAllProcessed(ref List<VoronoiTile> allTiles)
	//{
	//    foreach (var tile in allTiles)
	//    {
	//        if (!tile.processed) return false;
	//    }
	//    return true;
	//}
	//private void Update()
	//{
	//    transform.Rotate(Vector3.up * Time.deltaTime * rotationSpeed, Space.World);
	//    seconds -= Time.deltaTime;
	//    if (seconds <= 0)
	//    {
	//        Destroy(topWarmGizmo);
	//        Destroy(botWarmGizmo);
	//        Destroy(topColdGizmo);
	//        Destroy(botColdGizmo);
	//        Destroy(topTempGizmo);
	//        Destroy(botTempGizmo);
	//    }
	//}

	//public void DrawColdLat()
	//{
	//    Destroy(topColdGizmo);
	//    Destroy(botColdGizmo);

	//    float h = Vector2.Distance(new Vector2(0, coldLat), new Vector2(0, (float) size));
	//    float a = Mathf.Sqrt(h*(2*(float) size - h));

	//    float h2 = Vector2.Distance(new Vector2(0, -coldLat), new Vector2(0, (float)size));
	//    float a2 = Mathf.Sqrt(h2 * (2 * (float)size - h2));

	//    topColdGizmo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
	//    topColdGizmo.GetComponent<MeshRenderer>().sharedMaterial = coldMat;
	//    topColdGizmo.transform.localScale = new Vector3(a*2+0.65f, 0.04f, a*2+0.65f);
	//    topColdGizmo.transform.position = new Vector3(0,coldLat, 0);

	//    botColdGizmo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
	//    botColdGizmo.GetComponent<MeshRenderer>().sharedMaterial = coldMat;
	//    botColdGizmo.transform.localScale = new Vector3(a2 * 2 + 0.65f, 0.04f, a2 * 2 + 0.65f);
	//    botColdGizmo.transform.position = new Vector3(0, -coldLat, 0);
	//    seconds = 3f;
	//}

	//public void DrawTempLat()
	//{
	//    Destroy(topTempGizmo);
	//    Destroy(botTempGizmo);

	//    float h = Vector2.Distance(new Vector2(0, tempLat), new Vector2(0, (float)size));
	//    float a = Mathf.Sqrt(h * (2 * (float)size - h));

	//    float h2 = Vector2.Distance(new Vector2(0, -tempLat), new Vector2(0, (float)size));
	//    float a2 = Mathf.Sqrt(h2 * (2 * (float)size - h2));

	//    topTempGizmo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
	//    topTempGizmo.GetComponent<MeshRenderer>().sharedMaterial = temperateMat;
	//    topTempGizmo.transform.localScale = new Vector3(a * 2 + 0.65f, 0.04f, a * 2 + 0.65f);
	//    topTempGizmo.transform.position = new Vector3(0, tempLat, 0);

	//    botTempGizmo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
	//    botTempGizmo.GetComponent<MeshRenderer>().sharedMaterial = temperateMat;
	//    botTempGizmo.transform.localScale = new Vector3(a2 * 2 + 0.65f, 0.04f, a2 * 2 + 0.65f);
	//    botTempGizmo.transform.position = new Vector3(0, -tempLat, 0);
	//    seconds = 3f;
	//}

	//public void DrawWarmLat()
	//{
	//    Destroy(topWarmGizmo);
	//    Destroy(botWarmGizmo);

	//    float h = Vector2.Distance(new Vector2(0, warmLat), new Vector2(0, (float)size));
	//    float a = Mathf.Sqrt(h * (2 * (float)size - h));

	//    float h2 = Vector2.Distance(new Vector2(0, -warmLat), new Vector2(0, (float)size));
	//    float a2 = Mathf.Sqrt(h2 * (2 * (float)size - h2));

	//    topWarmGizmo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
	//    topWarmGizmo.GetComponent<MeshRenderer>().sharedMaterial = warmMat;
	//    topWarmGizmo.transform.localScale = new Vector3(a * 2 + 0.65f, 0.04f, a * 2 + 0.65f);
	//    topWarmGizmo.transform.position = new Vector3(0, warmLat, 0);

	//    botWarmGizmo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
	//    botWarmGizmo.GetComponent<MeshRenderer>().sharedMaterial = warmMat;
	//    botWarmGizmo.transform.localScale = new Vector3(a2 * 2 + 0.65f, 0.04f, a2 * 2 + 0.65f);
	//    botWarmGizmo.transform.position = new Vector3(0, -warmLat, 0);
	//    seconds = 3f;
	//}
}




















