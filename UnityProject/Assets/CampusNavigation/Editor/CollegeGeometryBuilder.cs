using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace CampusNavigation.Editor
{
    public static class CollegeGeometryBuilder
    {
        public static GameObject Prepare()
        {
            const string output="Assets/CampusNavigation/Generated/CollegeGeometry.prefab";
            var existing=AssetDatabase.LoadAssetAtPath<GameObject>(output);
            if(existing!=null)return existing;
            var active=SceneManager.GetActiveScene();
            var imported=EditorSceneManager.OpenScene("Assets/CampusNavigation/College/CollegeGeometry.unity",OpenSceneMode.Additive);
            var roots=imported.GetRootGameObjects();
            var container=new GameObject("College geometry");SceneManager.MoveGameObjectToScene(container,imported);
            foreach(var root in roots)root.transform.SetParent(container.transform,true);
            foreach(var collider in container.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(collider);
            foreach(var t in container.GetComponentsInChildren<Transform>(true))t.gameObject.layer=0;
            Directory.CreateDirectory("Assets/CampusNavigation/Generated");
            var prefab=PrefabUtility.SaveAsPrefabAsset(container,output);
            EditorSceneManager.CloseScene(imported,true);SceneManager.SetActiveScene(active);
            return prefab;
        }
    }
}
