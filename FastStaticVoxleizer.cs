//#define USE_LAZY_UPDATE

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FastStaticVoxleizer : Voxelizer {
	
	private int objCnt = 24;
	
	public bool startWithCollider = false;
	
	private List<GameObject> spatialBiggies;
	private List<Mesh> biggiesMesh;
	private int usualGoCount, lastGoCount;
	
	//making sure nothing happens
	protected override IEnumerator explodeCoRot (float power, Vector3 pos, float radius, float grav, float timeToDie)
	{
		yield return null;
	}
	public override void startWiggleAroundPt (Vector3 pt, float speed, float time){}
	protected override IEnumerator saturateCoRot (float grav, float timeToDie)
	{
		yield return null;
	}
	
	public override void genVoxels ()
	{
		List<Vector3> vert = new List<Vector3>();
		List<Vector2> uv = new List<Vector2>();
		List<int> triInd = new List<int>();
		
		spatialBiggies = new List<GameObject>();
		biggiesMesh = new List<Mesh>();
		
		int goCount = 0, totCnt = 0;
		
		base.genVoxels();
		
		foreach(NodeStruct tmp in listOfVoxels)
		{
			foreach(GameObject go in tmp.voxel)
			{
				goCount ++;
				
				Vector3 absPos = go.transform.position;
				Mesh tmpM = go.GetComponent<MeshFilter>().mesh;
				Vector3[] posTo = tmpM.vertices;
				Vector2[] tmpUv = tmpM.uv;
				int[] tmpTri = tmpM.triangles;
				int i;
				
				for(i=0; i<posTo.Length; i++)
					posTo[i] = absPos + posTo[i] * size;
				for(i=0; i<tmpTri.Length; i++)
					tmpTri[i] += totCnt;
				
				cpyDat<Vector3>(vert, posTo);
				cpyDat<Vector2>(uv, tmpUv);
				cpyDat<int>(triInd, tmpTri);
				
				totCnt+=posTo.Length;
				if(goCount > 2500)
				{
					createBiggie(triInd, vert, uv, voxParent.transform);
					goCount = 0;
					totCnt = 0;
				}
				
				Destroy(go.collider);
				Destroy(go);
			}
		}
		
		if(goCount > 0)
			createBiggie(triInd, vert, uv, voxParent.transform);
		
		usualGoCount = biggiesMesh[0].vertexCount/objCnt;
		lastGoCount = biggiesMesh[biggiesMesh.Count-1].vertexCount/objCnt;
		
		listOfVoxels.Clear();
		voxAll.Clear();
#if USE_LAZY_UPDATE
		StartCoroutine(lazyUpdate());
#endif
	}
	
	private void cpyDat<T>(List<T> master, T[] data)
	{
		for(int i=0; i<data.Length; i++)
			master.Add(data[i]);
	}
	
	private void createBiggie(List<int> iBuf, List<Vector3> vBuf, List<Vector2> uv, Transform par)
	{
		Mesh m = new Mesh();
		
		m.vertices = vBuf.ToArray();
		m.uv = uv.ToArray();
		m.triangles = iBuf.ToArray();
		m.RecalculateBounds();
		m.RecalculateNormals();
		m.Optimize();
		
		GameObject tmpBiggie = new GameObject("VOXEL_CRASH_FAST_STATIC");
		tmpBiggie.AddComponent<MeshFilter>();
		tmpBiggie.AddComponent<MeshRenderer>();
		
		tmpBiggie.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Diffuse"));
		tmpBiggie.GetComponent<MeshFilter>().mesh = m;
		
		if(startWithCollider)
		{
			tmpBiggie.AddComponent<MeshCollider>();
			tmpBiggie.GetComponent<MeshCollider>().sharedMesh = m;
			tmpBiggie.GetComponent<MeshCollider>().convex = true;
		}
		
		tmpBiggie.transform.parent = par;
		spatialBiggies.Add(tmpBiggie);
		biggiesMesh.Add(tmpBiggie.GetComponent<MeshFilter>().mesh);
		
		iBuf.Clear();
		vBuf.Clear();
		uv.Clear();
	}
	
	public override void setVoxelMat (Material mat)
	{
		if(mat == null)
			return;
		
		voxelMat = mat;
		foreach(GameObject tmp in spatialBiggies)
			tmp.renderer.material = mat;
	}
	
	public override void setVoxelsColor (Color col)
	{
		foreach(GameObject tmp in spatialBiggies)
			tmp.renderer.material.color = col;
		voxColor = col;
	}
	
#if USE_LAZY_UPDATE
	IEnumerator lazyUpdate()
	{
		while(true)
		{
			if(!isWiggling)
			{
				yield return null;
				continue;
			}
			
			int startInd = 0;
			int endInd = startInd + usualGoCount;
			
			
			for(int i=0; i<biggiesMesh.Count; i++)
			{
				Mesh tmp = biggiesMesh[i];
				Vector3[] verts = tmp.vertices;
				float dt = Time.deltaTime * biggiesMesh.Count;
				
				if(i==biggiesMesh.Count-1)
					endInd = startInd + lastGoCount;
				
				int cnt=0;
				for(int j=startInd; j<endInd; j++)
				{
					wiggleOffset[j] += dt;
					for(int k=0; k<objCnt; k++)
						verts[cnt++] += Mathf.Sin(wiggleOffset[j]/wigTime*Mathf.PI) * wigSpeed;
				}
				tmp.vertices = verts;
				yield return null;
				
				startInd += usualGoCount;
				endInd = startInd + usualGoCount;
				
			}
		}
	}
	
	new void Update()
	{
		
	}
#else
	new void Update ()
	{
		if(!isWiggling)
			return;
		
		int startInd = 0;
		int endInd = startInd + usualGoCount;
		
		for(int i=0; i<biggiesMesh.Count; i++)
		{
			Mesh tmp = biggiesMesh[i];
			Vector3[] verts = tmp.vertices;
			
			if(i==biggiesMesh.Count-1)
				endInd = startInd + lastGoCount;
			
			int cnt=0;
			for(int j=startInd; j<endInd; j++)
			{
				wiggleOffset[j] += Time.deltaTime;
				for(int k=0; k<objCnt; k++)
					verts[cnt++] += Mathf.Sin(wiggleOffset[j]/wigTime*Mathf.PI) * wigSpeed[j];
			}
			
			startInd += usualGoCount;
			endInd = startInd + usualGoCount;
			
			tmp.vertices = verts;
		}
	}
#endif

}
