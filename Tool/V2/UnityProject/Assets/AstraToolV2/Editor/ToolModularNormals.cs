using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>Keep authored base shading when a localized sculpt key is activated.</summary>
public sealed class ToolModularNormals : AssetPostprocessor
{
    // Unity's recalculated shape normals can disagree with imported base normals,
    // changing the shading even on unmoved petals, eyes and hair. Rotate the authored
    // normals by the actual geometric change; an unmoved surface gets zero delta.
    void OnPostprocessModel(GameObject model)
    {
        if(!assetPath.EndsWith("/AstraToolV2/Models/ModularCast.fbx"))return;
        foreach(var renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            var mesh=renderer.sharedMesh;if(!mesh||mesh.blendShapeCount==0)continue;
            var basis=mesh.vertices;var authored=mesh.normals;var triangles=mesh.triangles;
            if(authored.Length!=basis.Length)continue;
            var baseNormals=Normals(basis,triangles);var frames=new List<Frame>();
            for(int key=0;key<mesh.blendShapeCount;key++)for(int frame=0;frame<mesh.GetBlendShapeFrameCount(key);frame++)
            {
                var f=new Frame{name=mesh.GetBlendShapeName(key),weight=mesh.GetBlendShapeFrameWeight(key,frame),vertices=new Vector3[basis.Length],normals=new Vector3[basis.Length],tangents=new Vector3[basis.Length]};
                mesh.GetBlendShapeFrameVertices(key,frame,f.vertices,f.normals,f.tangents);
                var positions=new Vector3[basis.Length];
                for(int i=0;i<basis.Length;i++)positions[i]=basis[i]+f.vertices[i];
                var deformed=Normals(positions,triangles);
                for(int i=0;i<basis.Length;i++)
                {
                    var rotation=baseNormals[i].sqrMagnitude<.1f||deformed[i].sqrMagnitude<.1f?Quaternion.identity:Quaternion.FromToRotation(baseNormals[i],deformed[i]);
                    f.normals[i]=rotation*authored[i]-authored[i];f.tangents[i]=Vector3.zero;
                }
                frames.Add(f);
            }
            mesh.ClearBlendShapes();
            foreach(var f in frames)mesh.AddBlendShapeFrame(f.name,f.weight,f.vertices,f.normals,f.tangents);
        }
    }
    static Vector3[] Normals(Vector3[] vertices,int[] triangles)
    {
        var result=new Vector3[vertices.Length];
        for(int i=0;i<triangles.Length;i+=3)
        {
            int a=triangles[i],b=triangles[i+1],c=triangles[i+2];var n=Vector3.Cross(vertices[b]-vertices[a],vertices[c]-vertices[a]);
            result[a]+=n;result[b]+=n;result[c]+=n;
        }
        for(int i=0;i<result.Length;i++)result[i].Normalize();return result;
    }
    sealed class Frame{public string name;public float weight;public Vector3[] vertices,normals,tangents;}
}
