using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NavMeshSourceTagWithAreas : MonoBehaviour
{
	public static List<Tuple<MeshFilter, int>> m_Meshes =
	    new List<Tuple<MeshFilter, int>>();
	public static List<Tuple<Terrain, int>> m_Terrains =
	    new List<Tuple<Terrain, int>>();

	[SerializeField]
	public int areaNum = -1;

	void OnEnable()
	{
		if(areaNum == -1) {return;}
		addToList();
	}
	
	public void addToList()
	{
		var m = GetComponent<MeshFilter>();
		if (m != null)
		{
			m_Meshes.Add(new Tuple<MeshFilter, int>(m, areaNum));
		}

		var t = GetComponent<Terrain>();
		if (t != null)
		{
			m_Terrains.Add(
			    new Tuple<Terrain, int>(t, areaNum));
		}
	}

	void OnDisable()
	{
		var m = GetComponent<MeshFilter>();
		if (m != null)
		{
			m_Meshes.Remove(new Tuple<MeshFilter, int>(
			    m, areaNum));
		}

		var t = GetComponent<Terrain>();
		if (t != null)
		{
			m_Terrains.Remove(
			    new Tuple<Terrain, int>(t, areaNum));
		}
	}

	public static void Collect(ref List<NavMeshBuildSource> sources)
	{
		sources.Clear();

		for (var i = 0; i < m_Meshes.Count; ++i)
		{
			var mf = m_Meshes[i];
			if (mf.Item1 == null) continue;

			var m = mf.Item1.sharedMesh;
			if (m == null) continue;

			var s          = new NavMeshBuildSource();
			s.shape        = NavMeshBuildSourceShape.Mesh;
			s.sourceObject = m;
			s.transform    = mf.Item1.transform.localToWorldMatrix;
			s.area         = mf.Item2;
			sources.Add(s);
		}

		for (var i = 0; i < m_Terrains.Count; ++i)
		{
			var t = m_Terrains[i];
			if (t.Item1 == null) continue;

			var s          = new NavMeshBuildSource();
			s.shape        = NavMeshBuildSourceShape.Terrain;
			s.sourceObject = t.Item1.terrainData;
			// Terrain system only supports translation - so we pass translation
			// only to back-end
			s.transform = Matrix4x4.TRS(
			    t.Item1.transform.position, Quaternion.identity, Vector3.one);
			s.area = t.Item2;
			sources.Add(s);
		}
	}
}
