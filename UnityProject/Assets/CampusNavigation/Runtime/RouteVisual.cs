using System.Collections.Generic;
using UnityEngine;
namespace CampusNavigation
{
    public sealed class RouteVisual : MonoBehaviour
    {
        public NavigationSession nav;public Transform mapRoot;public Material material;
        readonly List<GameObject> arrows=new List<GameObject>();Mesh mesh;int version=-1,floor=-99;float nextUpdate;
        void Awake()
        {
            mesh=new Mesh{name="Arrow"};mesh.vertices=new[]{new Vector3(-.24f,0,-.28f),new Vector3(0,0,-.12f),new Vector3(.24f,0,-.28f),new Vector3(0,0,.3f)};
            mesh.triangles=new[]{0,3,1,1,3,2};mesh.RecalculateNormals();
        }
        void LateUpdate()
        {
            bool visible=nav!=null&&nav.pose.Ready&&nav.Active&&nav.Route!=null&&nav.Route.Found;
            if(!visible){foreach(var a in arrows)a.SetActive(false);return;}
            if(version!=nav.Version||floor!=nav.pose.Floor||Time.unscaledTime>=nextUpdate)
            {
                version=nav.Version;floor=nav.pose.Floor;nextUpdate=Time.unscaledTime+.2f;int used=0;
                var r=nav.Route;
                for(int i=Mathf.Max(1,nav.Next);i<r.points.Count;i++)
                {
                    if(r.floors[i]!=floor||r.floors[i-1]!=floor)continue;
                    Vector3 a=ARPoseSource.ToVector(r.points[i-1]),b=ARPoseSource.ToVector(r.points[i]),d=b-a;
                    if(d.sqrMagnitude<.01f)continue;
                    for(float t=.3f;t<d.magnitude;t+=.85f)
                    {
                        Vector3 p=a+d.normalized*t;
                        if(Vector3.Distance(p,ARPoseSource.ToVector(nav.pose.Position))>18)continue;
                        if(used==arrows.Count)
                        {
                            var go=new GameObject("Route arrow");go.transform.SetParent(mapRoot,false);
                            go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=material;arrows.Add(go);
                        }
                        var ar=arrows[used++];ar.SetActive(true);ar.transform.localPosition=p+Vector3.up*.04f;ar.transform.localRotation=Quaternion.LookRotation(d,Vector3.up);
                        if(used>=160)break;
                    }
                    if(used>=160)break;
                }
                for(int i=used;i<arrows.Count;i++)arrows[i].SetActive(false);
            }
        }
        void OnDestroy(){if(mesh!=null)Destroy(mesh);}
    }
}
