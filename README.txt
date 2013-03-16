Voxel for Unity
===============
Usage:
1. Create an empty parent object and attach the script to that.
2. Drag the model into the empty parent object(making it a child of the empty object)


1. Voxelizer - Converts a mesh into voxels. Each cube is a separate gameobject

	+ Can explode
	+ Other cool effects can be written
	- Increases the number of draw calls

2. FastStatic Voxelizer - Converts a mesh into voxels but then each cube is part of a big mesh(allows for fast rendering)

	+ Looks the same as Voxelizer
	- Not as fluid as the Voxelizer since all the cubes are part of a few big meshes

3. QuickVoxelswitcher - quickly switches between Voxelizer and FaststaticVoxelizer

Please check out my Round 5 BVW world that makes use of this script at http://projectscrash.webs.com/apps/blog/