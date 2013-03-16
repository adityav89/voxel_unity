using UnityEngine;
using System.Collections;

public class QuickVoxelSwitcher : MonoBehaviour {
	
	public Voxelizer[] dynamicVoxelObjs;
	public FastStaticVoxleizer[] fastStaticVoxelObjs;
	
	// Use this for initialization
	void Start () {
		
		showFastOnes();
	}
	
	public void showFastOnes()
	{
		for(int i=0; i<fastStaticVoxelObjs.Length; i++)
		{
			fastStaticVoxelObjs[i].gameObject.SetActiveRecursively(true);
			dynamicVoxelObjs[i].gameObject.SetActiveRecursively(false);
		}
	}
	
	public void showSlowOnes()
	{
		for(int i=0; i<fastStaticVoxelObjs.Length; i++)
		{
			fastStaticVoxelObjs[i].gameObject.SetActiveRecursively(false);
			dynamicVoxelObjs[i].gameObject.SetActiveRecursively(true);
		}
	}
	
	public bool shake(Vector3 pos)
	{
		if(!isInUse)
		{
			//explode(pos);
			StartCoroutine(shakeCo(pos));
			return true;
		}
		return false;
	}
	
	IEnumerator shakeCo(Vector3 pos)
	{
		isInUse = true;
		showSlowOnes();
		
		foreach(Voxelizer tmp in dynamicVoxelObjs)
			tmp.startWiggleAroundPt(pos, 4, 0.5f);
		
		yield return new WaitForSeconds(0.6f);
		
		foreach(Voxelizer tmp in dynamicVoxelObjs)
			tmp.stopWiggle();
		
		showFastOnes();
		isInUse = false;
	}
	
	public bool explode(Vector3 pos)
	{
		if(!isInUse)
		{
			StartCoroutine(explodeCo(pos, 100, 30, false, 1f, 0.4f, false));
			return true;
		}
		return false;
	}
	
	public bool isInUse = false;
	IEnumerator explodeCo(Vector3 pos, float pow, float rad, bool slowMo, float eTime, float mTime, bool dest)
	{
		pos += new Vector3(1, 0, 0);
		isInUse = true;
		showSlowOnes();
		if(slowMo)
			Time.timeScale = 0.2f;
		
		float explTime = eTime;
		float magTime = mTime;
		
		foreach(Voxelizer tmp in dynamicVoxelObjs)
			tmp.explode(pow, pos, rad, 80f, explTime);
		
		yield return new WaitForSeconds(explTime-0.2f);
		Time.timeScale = 1;
		
		if(!dest)
		{
			foreach(Voxelizer tmp in dynamicVoxelObjs)
				tmp.magnetBack(magTime);
			
			yield return new WaitForSeconds(magTime);
		}
		
		
		if(dest)
		{
			Destroy(gameObject);
		}
		isInUse = false;
		showFastOnes();
	}
	
	public bool explodeForever()
	{
		if(!isInUse)
		{
			transform.parent = GameObject.Find("root").transform;
			StartCoroutine(explodeCo(transform.position, 200, 40, false, 6f, 0.2f, true));
			return true;
		}
		return false;
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
