#ifndef TERRAINMESHBLEND_INCLUDED
#define TERRAINMESHBLEND_INCLUDED
inline float3 combineNormals (float3 n1, float3 n2){
	n1 += float3(0, 0, 1);
	n2 *= float3(-1, -1, 1);
    return n1 * dot(n1, n2) / n1.z - n2;
}
inline float3 transformNormals (float3 tn, float3 n2, float3 tng){
	float3 btng =  cross(tn, tng);
	float3x3 nmatrix = float3x3(tng,btng,tn);
	return  mul(nmatrix,n2);
}
#endif // terrainmeshblend_included