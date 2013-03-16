using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Voxelizer : MonoBehaviour {
	
	public float size=0.1f;
	public Color startColor = Color.red;
	public bool wiggleAtStart = false;
	public bool fixVoxRotation = false;
	public Material voxelMat;
	public float closenessThreshold = 0.002f;
	public GameObject[] startColliders;
	
	protected Color voxColor;
	protected List<NodeStruct> listOfVoxels;
	protected Renderer[] oriRenderers;
	protected List<GameObject> voxAll;
	protected GameObject voxParent;
	protected Collider[] virtColliders;
	
	protected bool isWiggling = false;
	protected Vector3[] wigSpeed;
	protected float[] wiggleOffset;
	protected float wigTime;
	
	public bool isExploding = false;
	protected Vector3[] explVel;
	protected bool[] explNotEffected;
	protected float explGravMod = 0;
	
	protected bool isMagging = false;
	protected float magTime=-1, currMagTime=-1;
	protected Vector3[] magStartPos;
	
	protected static int totalVoxelCount=0;
	
	//so that colliders in Fast static are NOT created during Start
	public void Awake()
	{
		genVoxels();
		print(totalVoxelCount + " includes FAST_STATIC_VOXELS");
	}
	
	// Use this for initialization
	void Start () 
	{	
		hideOriMesh();
		if(wiggleAtStart)
			startWiggleInDir(new Vector3(Random.value, Random.value, Random.value), 0.2f, 2);
		setVoxelMat(voxelMat);
		setVoxelsColor(startColor);
		
		if(virtColliders == null)
			virtColliders = new Collider[0];
		
		StartCoroutine(LateStart());		
	}
	
	IEnumerator LateStart()
	{
		yield return null;
		genVirtualColliders(startColliders);
	}
	
	public void genVirtualColliders(GameObject[] phyColliders)
	{
		if(phyColliders == null)
			return;
		
		List<Collider> collList = new List<Collider>();
		
		foreach(GameObject tmp in phyColliders)
		{
			Collider[] coll = tmp.GetComponentsInChildren<Collider>();
			
			if(coll == null)
				continue;
			
			foreach(Collider c in coll)
				collList.Add(c);
		}
		
		virtColliders = collList.ToArray();
	}
	
	// Update is called once per frame
	public void Update () {
		
		updateVoxels();
	}
	
	virtual public void setVoxelsColor(Color col)
	{
		foreach(GameObject tmp in voxAll)
			tmp.renderer.material.color = col;
		voxColor = col;
	}
	
	public Color voxelColor
	{
		get{return voxColor;}
	}
	
	public void hideOriMesh()
	{
		foreach(Renderer tmp in oriRenderers)
			tmp.enabled = false;
	}
	
	public void showOriMesh()
	{
		foreach(Renderer tmp in oriRenderers)
			tmp.enabled = true;
	}
	
	virtual public void setVoxelMat(Material mat)
	{
		if(mat == null)
			return;
		
		voxelMat = mat;
		foreach(GameObject tmp in voxAll)
			tmp.renderer.material = mat;
	}
	
	virtual public void genVoxels()	
	{
		oriRenderers = GetComponentsInChildren<Renderer>();
		listOfVoxels = new List<NodeStruct>();
		voxAll = new List<GameObject>();
		voxParent = new GameObject("VOXEL_PARENT");
		
		populateDS(transform, PrimitiveType.Cube);
		wiggleOffset = new float[voxAll.Count];
		wigSpeed = new Vector3[voxAll.Count];
		explVel = new Vector3[voxAll.Count];
		explNotEffected = new bool[voxAll.Count];
		magStartPos = new Vector3[voxAll.Count];
		
		updateVoxels();
		voxParent.transform.parent = transform;
	}
	
	virtual public void startWiggleAroundPt(Vector3 pt, float speed, float time)
	{
		int x=0;
		
		isWiggling = true;
		foreach(NodeStruct tmp in listOfVoxels)
		{
			GameObject[] go = tmp.voxel;
			for(int i=0; i<go.Length; i++, x++)
			{
				wiggleOffset[x] = Random.value;
				wigSpeed[x] = speed * Vector3.Normalize(pt-go[i].transform.position);
			}
		}
		
		wigTime = time;
	}
	
	virtual public void startWiggleInDir(Vector3 dir, float speed, float time)
	{
		dir.Normalize();
		isWiggling = true;
		for(int i=0; i<wigSpeed.Length; i++)
			wigSpeed[i] = speed * dir;
		wigTime = time;
		for(int i=0; i<wiggleOffset.Length; i++)
			wiggleOffset[i] = Random.value;
	}
	
	public void explode(float power, Vector3 pos, float radius)
	{
		explode(power, pos, radius, 40f, 4);
	}
	
	public void explode(float power, Vector3 pos, float radius, float timeToDie)
	{
		explode(power, pos, radius, 40f, timeToDie);
	}
	
	public void explode(float power, Vector3 pos, float radius, float grav, float timeToDie)
	{
		StartCoroutine(explodeCoRot(power, pos, radius, grav, timeToDie));
	}
	
	virtual protected IEnumerator explodeCoRot(float power, Vector3 pos, float radius, float grav, float timeToDie)
	{
		isExploding = true;
		
		explGravMod = grav;
		int x=0;
		foreach(NodeStruct tmp in listOfVoxels)
		{
			GameObject[] go = tmp.voxel;
			for(int i=0; i<go.Length; i++, x++)
			{
				float dist = Vector3.Distance(pos, go[i].transform.position);
				if(dist < radius)
				{
					float modder = (x%2==0?0.85f:1f);
					Vector3 modVec = Vector3.zero;
					if(x%2 == 0)
						modVec = new Vector3(0, power*0.4f, 0);
					
					explNotEffected[x] = false;					
					explVel[x] = ((pos-go[i].transform.position).normalized + Random.value * new Vector3(Random.value, Random.value, Random.value)).normalized
						* (1-(dist/radius) * power*modder) + modVec;
				}
				else 
				{
					explNotEffected[x] = true;
					explVel[x] = Vector3.zero;
				}
			}
		}
		
		yield return new WaitForSeconds(timeToDie);
		isExploding = false;
		foreach(GameObject tmp in voxAll)
			tmp.transform.eulerAngles = Vector3.zero;
	}
	
	public void saturate(float grav, float timeToDie)
	{
		StartCoroutine(saturateCoRot(grav, timeToDie));
	}
	
	virtual protected IEnumerator saturateCoRot(float grav, float timeToDie)
	{
		isExploding = true;
		
		explGravMod = grav;
		
		int x=0;
		foreach(NodeStruct tmp in listOfVoxels)
		{
			GameObject[] go = tmp.voxel;
			for(int i=0; i<go.Length; i++, x++)
			{
				explNotEffected[x] = false;
				explVel[x] = new Vector3(0,0,0);
			}
		}
		
		yield return new WaitForSeconds(timeToDie);
		isExploding = false;
		foreach(GameObject tmp in voxAll)
			tmp.transform.eulerAngles = Vector3.zero;
	}
	
	public void magnetBack(float time)
	{		
		StartCoroutine(magnetBackCoRot(time));
	}
	
	virtual protected IEnumerator magnetBackCoRot(float time)
	{
		stopWiggle();
		
		currMagTime = 0;
		magTime = time;
		
		isExploding = false;
		isMagging = true;
		
		int x=0;
		foreach(NodeStruct tmp in listOfVoxels)
		{
			GameObject[] go = tmp.voxel;
			for(int i=0; i<go.Length; i++, x++)
				magStartPos[x] = go[i].transform.position;
		}
			
		
		yield return new WaitForSeconds(time);
		isMagging = false;
	}
	
	virtual public void stopWiggle()
	{
		isWiggling = false;
	}
	
	//little messy BUT making sure each box is prosessed only once, NEED TO OPTIMIZE MORE
	protected void updateVoxels()
	{
		int x = 0;
		
		foreach(NodeStruct tmp in listOfVoxels)
		{
			Matrix4x4 parentPos = tmp.parent.localToWorldMatrix;
			Vector3[] ps = tmp.mPos.vertices;
			GameObject[] go = tmp.voxel;
			for(int i=0; i<go.Length; i++, x++)
			{
				if(isExploding && !explNotEffected[x])
				{
					bool calcFlag = true;
					foreach(Collider c in virtColliders)
						if(c.bounds.Intersects(go[i].collider.bounds))
							calcFlag = false;
					if(calcFlag)
					{
						go[i].transform.Rotate(new Vector3(1,1,1) * Time.deltaTime * 1000);
						go[i].transform.position += explVel[x] * Time.deltaTime;
						explVel[x].y -= explGravMod * Time.deltaTime;
					}
				}
				else if(isMagging)
				{
					Vector3 toPos = parentPos.MultiplyPoint3x4(ps[i]);
					Vector3 fromPos = magStartPos[x];
					go[i].transform.position = Vector3.Lerp(fromPos, toPos, (currMagTime/magTime));
				}
				else
				{
					go[i].transform.position = parentPos.MultiplyPoint3x4(ps[i]);
					if(isWiggling)
					{
						wiggleOffset[x] += Time.deltaTime;
						go[i].transform.position += Mathf.Sin(wiggleOffset[x]/wigTime*Mathf.PI) * wigSpeed[x];
					}
				}
				
				if(fixVoxRotation)
					go[i].transform.eulerAngles = Vector3.zero;
			}
		}
		
		if(isMagging && currMagTime < magTime)
			currMagTime += Time.deltaTime;
	}
	
	private void genCubes(Transform ch, PrimitiveType pType)
	{
			if(ch.collider != null)
				Destroy(ch.collider);
			
			MeshFilter mf = ch.GetComponent<MeshFilter>();
			SkinnedMeshRenderer sm = ch.GetComponent<SkinnedMeshRenderer>();
			Mesh m = null;
			
			if(mf != null)
				m = mf.mesh;
			else if(sm != null)
				m = sm.sharedMesh;
			
			if(m != null)
			{
				Vector3[] pos = m.vertices;
				
				if(pos.Length > 0)
				{
					List<GameObject> go = new List<GameObject>();
					
					for(int i=0; i<pos.Length; i++)
					{
						//need a faster way to optimize but this is a one-timer
						bool hasIt = false;
						for(int j=0; j<i; j++)
						{
							if(Vector3.Distance(pos[i], pos[j]) < closenessThreshold)
							{
								hasIt = true;
								break;
							}
						}
						if(hasIt)
							continue;
						
						GameObject tmpGo = GameObject.CreatePrimitive(pType);
						tmpGo.transform.localScale = new Vector3(size,size,size);
						tmpGo.transform.parent = voxParent.transform;
						Destroy(tmpGo.collider);
						tmpGo.name = "VOXEL_CRASH_BASIC";
						
						voxAll.Add(tmpGo);
						go.Add(tmpGo);
						totalVoxelCount++;
					}
					
					GameObject[] goArr = go.ToArray();
					listOfVoxels.Add(new NodeStruct(ch, m, goArr));
				}
			}
	}
	
	private void populateDS(Transform node, PrimitiveType pType)
	{
		foreach(Transform ch in node)
		{
			populateDS(ch, pType);
			genCubes(ch, pType);
		}
	}
}

public class NodeStruct
{
	public Transform parent;
	public Mesh mPos;
	public GameObject[] voxel;
	
	public NodeStruct(Transform p, Mesh m, GameObject[] vx)
	{
		parent = p;
		mPos = m;
		voxel = vx;
	}
}