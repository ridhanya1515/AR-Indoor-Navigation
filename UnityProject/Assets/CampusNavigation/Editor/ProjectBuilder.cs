using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Management;
namespace CampusNavigation.Editor
{
    public static class ProjectBuilder
    {
        const string Root="Assets/CampusNavigation";
        const string ScenePath=Root+"/Scenes/CampusNavigation.unity";
        [MenuItem("Campus Navigation/1 - Create or open project scene")]
        public static void CreateScene()
        {
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            Directory.CreateDirectory(Root+"/Scenes");Directory.CreateDirectory(Root+"/Generated");AssetDatabase.Refresh();
            Configure();
            var geometry=CollegeGeometryBuilder.Prepare();
            if(File.Exists(ScenePath)){EditorSceneManager.OpenScene(ScenePath);Debug.Log("Scene opened. Use Campus Navigation > 2 - Build Android APK.");return;}
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var app=new GameObject("Campus Navigation").AddComponent<CampusApplication>();
            app.collegeGeometry=geometry;
            app.bundledMap=AssetDatabase.LoadAssetAtPath<TextAsset>(Root+"/Resources/DemoMap.json");
            NavGraph.Validate(JsonUtility.FromJson<CampusMap>(app.bundledMap.text));
            var mat=new Material(Shader.Find("CampusNavigation/Route")){name="Route Blue",color=new Color(.02f,.4f,1)};
            string materialPath=Root+"/Generated/Route.mat";
            if(AssetDatabase.LoadAssetAtPath<Material>(materialPath)==null)AssetDatabase.CreateAsset(mat,materialPath);
            else UnityEngine.Object.DestroyImmediate(mat);
            app.routeMaterial=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            EditorSceneManager.SaveScene(scene,ScenePath);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};AssetDatabase.SaveAssets();
            Selection.activeGameObject=app.gameObject;
            Debug.Log("Scene ready. Play for keyboard simulation, or build Android for live camera tracking.");
        }
        static void Configure()
        {
            // This project uses the built-in renderer; no URP assets are necessary.
            if(GraphicsSettings.defaultRenderPipeline!=null)throw new InvalidOperationException("Open the supplied separate UnityProject. It uses the built-in renderer.");
            PlayerSettings.companyName="CampusNavigation";PlayerSettings.productName="Campus AR Workflow";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android,"com.campusnavigation.workflow");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android,ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion=AndroidSdkVersions.AndroidApiLevel25;
            PlayerSettings.Android.targetSdkVersion=AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.defaultInterfaceOrientation=UIOrientation.Portrait;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android,false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,new[]{GraphicsDeviceType.OpenGLES3});
            // Local ESP32 HTTP endpoint. Use this APK on a trusted demo LAN.
            PlayerSettings.insecureHttpOption=InsecureHttpOption.AlwaysAllowed;
            PlayerSettings.Android.forceInternetPermission=true;
            var ps=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            var input=ps.FindProperty("activeInputHandler");if(input!=null){input.intValue=1;ps.ApplyModifiedPropertiesWithoutUndo();}
            XRGeneralSettingsPerBuildTarget perTarget;
            if(!EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey,out perTarget)||perTarget==null)
            {
                perTarget=ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                AssetDatabase.CreateAsset(perTarget,Root+"/Generated/XRSettings.asset");
                EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey,perTarget,true);
            }
            if(!perTarget.HasSettingsForBuildTarget(BuildTargetGroup.Android))perTarget.CreateDefaultSettingsForBuildTarget(BuildTargetGroup.Android);
            if(!perTarget.HasManagerSettingsForBuildTarget(BuildTargetGroup.Android))perTarget.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.Android);
            var settings=perTarget.SettingsForBuildTarget(BuildTargetGroup.Android);settings.InitManagerOnStart=true;
            if(!XRPackageMetadataStore.AssignLoader(settings.Manager,"UnityEngine.XR.ARCore.ARCoreLoader",BuildTargetGroup.Android))throw new InvalidOperationException("Could not enable ARCore. Open XR Plug-in Management and enable ARCore for Android.");
            EditorUtility.SetDirty(settings);EditorUtility.SetDirty(settings.Manager);EditorUtility.SetDirty(perTarget);AssetDatabase.SaveAssets();
        }
        [MenuItem("Campus Navigation/2 - Build Android APK")]
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode first.");
            if(!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android,BuildTarget.Android))throw new InvalidOperationException("Install Android Build Support, SDK/NDK and OpenJDK in Unity Hub.");
            if(EditorUserBuildSettings.activeBuildTarget!=BuildTarget.Android)
            {
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android,BuildTarget.Android);
                Debug.Log("After Android recompilation, choose Campus Navigation > 2 - Build Android APK again.");return;
            }
            if(!File.Exists(ScenePath))CreateScene();
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            Configure();Directory.CreateDirectory("Builds");
            var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},target=BuildTarget.Android,locationPathName="Builds/CampusAR_Workflow.apk",options=BuildOptions.None});
            File.WriteAllText("Builds/BuildResult.txt",result.summary.result+"\nErrors: "+result.summary.totalErrors+"\nWarnings: "+result.summary.totalWarnings);
            if(result.summary.result!=BuildResult.Succeeded)throw new Exception("Build failed. Read the first red Console error.");
            Debug.Log("APK built: "+Path.GetFullPath("Builds/CampusAR_Workflow.apk")+". Phone and hardware acceptance tests remain required.");
        }
        [MenuItem("Campus Navigation/3 - Validate bundled map")]
        public static void ValidateMap()
        {
            var asset=AssetDatabase.LoadAssetAtPath<TextAsset>(Root+"/Resources/DemoMap.json");var data=JsonUtility.FromJson<CampusMap>(asset.text);var graph=new NavGraph(data);
            var start=data.nodes.First(n=>n.checkpoint);
            foreach(var goal in data.nodes.Where(n=>n.destination))
            {float cost;if(graph.FindNodes(start.id,goal.id,out cost)==null)throw new Exception("Unreachable room: "+goal.label);}
            Debug.Log("Map structure valid. All destinations reachable with sensors disabled. Survey and phone testing still required.");
        }
    }
}
