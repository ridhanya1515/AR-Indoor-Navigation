using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

namespace CampusAR.Editor
{
    // Adds ground-floor interiors to six named buildings. Never regenerates CSE.
    public static class OtherBlocksSetup
    {
        const string RootName = "Other Blocks - Ground Floor Rooms";
        const string LayoutPath = "Assets/IndoorNavigation/OtherBlocks/OtherBlocksLayout.json";
        [Serializable] sealed class Layout { public CampusSolid[] replacements; public CampusData addition; }

        [MenuItem("Campus AR/Other Blocks/Add Rooms in Scene Copy")]
        public static void AddRooms()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play mode first.");
            var source = SceneManager.GetActiveScene();
            var app = Find<CampusApp>(source);
            var preview = Find<ScenePreview>(source);
            if (app == null || preview == null || app.campusJson == null)
                throw new InvalidOperationException("Open your current campus scene with the CSE block you want to retain.");
            if (preview.transform.Find(RootName) != null)
            { Debug.Log("Other-block rooms already exist in this scene. No duplicate was added."); return; }
            if (app.authoredEnvironment != null && app.authoredEnvironment != preview.transform)
                throw new InvalidOperationException("The scene has a different authored environment. No changes made.");
            var text = AssetDatabase.LoadAssetAtPath<TextAsset>(LayoutPath);
            if (text == null) throw new InvalidOperationException("Missing OtherBlocksLayout.json. Copy the complete update Assets folder.");
            var layout = JsonUtility.FromJson<Layout>(text.text);
            var old = JsonUtility.FromJson<CampusData>(app.campusJson.text);
            var replacementNames = new HashSet<string>(layout.replacements.Select(s => s.name));
            foreach (var expected in layout.replacements)
            {
                var matching = old.solids.Where(s => s.name == expected.name).ToArray();
                if (matching.Length != 1 || !SameSolid(matching[0], expected))
                    throw new InvalidOperationException(expected.name + " differs from the supplied layout. Nothing changed; this block needs fitting first.");
                Locate(preview.transform, expected);
            }
            if (old.nodes.Any(n => n.id.StartsWith("OB_")))
                throw new InvalidOperationException("This map already contains other-block routes. Open its existing completed scene.");
            var map = new CampusData {
                note = old.note + "\nOther blocks: proposed ground-floor rooms; original CSE retained.",
                nodes = old.nodes.Concat(layout.addition.nodes).ToArray(),
                edges = old.edges.Concat(layout.addition.edges).ToArray(),
                solids = old.solids.Where(s => !replacementNames.Contains(s.name)).Concat(layout.addition.solids).ToArray(),
                labels = (old.labels ?? new CampusLabel[0]).Concat(layout.addition.labels).ToArray()
            };
            // No existing graph node or edge is edited or removed.
            var graph = new NavigationGraphManager(map);
            ValidateConnected(graph);
            var palette = app.palette.ToDictionary(p => p.key, p => p.material);
            foreach (var s in layout.addition.solids)
                if (!palette.ContainsKey(s.color) || palette[s.color] == null)
                    throw new InvalidOperationException("Missing material: " + s.color);
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string sourcePath = source.path;
            if (string.IsNullOrEmpty(sourcePath))
                throw new InvalidOperationException("Save your campus scene first, then run Add Rooms again.");
            Directory.CreateDirectory("Assets/IndoorNavigation/Scenes");
            Directory.CreateDirectory("Assets/IndoorNavigation/Generated");
            string output = AssetDatabase.GenerateUniqueAssetPath("Assets/IndoorNavigation/Scenes/" +
                (app.arMode ? "ARNavigation" : "CampusSimulation") + "_AllBlocks.unity");
            string json = AssetDatabase.GenerateUniqueAssetPath("Assets/IndoorNavigation/Generated/AllBlocks_Map.json");
            if (!EditorSceneManager.SaveScene(source, output, true))
                throw new IOException("Could not create the scene copy.");
            try
            {
                AssetDatabase.Refresh();
                var scene = EditorSceneManager.OpenScene(output);
                app = Find<CampusApp>(scene); preview = Find<ScenePreview>(scene);
                string before = CSESignature(preview.transform);
                foreach (var expected in layout.replacements)
                {
                    UnityEngine.Object.DestroyImmediate(Locate(preview.transform, expected).gameObject);
                    HideOldDecoration(preview.transform, expected);
                }
                var root = new GameObject(RootName).transform;
                root.SetParent(preview.transform, false);
                foreach (var s in layout.addition.solids)
                {
                    var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    go.name = s.name; go.layer = 8; go.transform.SetParent(root, false);
                    go.transform.localPosition = new Vector3(s.x, s.y, s.z);
                    go.transform.localScale = new Vector3(s.sx, s.sy, s.sz);
                    go.transform.localRotation = Quaternion.Euler(s.rx, 0, 0);
                    go.GetComponent<Renderer>().sharedMaterial = palette[s.color];
                    go.GetComponent<Collider>().enabled = s.collision;
                }
                // Authored TextMeshes are visible in Scene view; runtime map labels come from JSON.
                foreach (var label in layout.addition.labels)
                {
                    var go = new GameObject("OB Room Label " + label.text.Replace('\n', ' '));
                    go.transform.SetParent(root, false);
                    go.transform.localPosition = new Vector3(label.x, label.y, label.z);
                    go.transform.localRotation = Quaternion.Euler(90, 0, 0);
                    var mesh = go.AddComponent<TextMesh>();
                    mesh.text = label.text; mesh.anchor = TextAnchor.MiddleCenter;
                    mesh.alignment = TextAlignment.Center; mesh.fontSize = 48;
                    mesh.characterSize = .055f; mesh.color = new Color(.08f, .12f, .18f);
                }
                if (CSESignature(preview.transform) != before)
                    throw new InvalidOperationException("CSE preservation check failed. Source scene restored.");
                Physics.SyncTransforms();
                ValidateNewRoutes(graph, layout.addition.edges, preview.transform);
                File.WriteAllText(json, JsonUtility.ToJson(map, true));
                AssetDatabase.Refresh();
                app.campusJson = AssetDatabase.LoadAssetAtPath<TextAsset>(json);
                if (app.campusJson == null) throw new IOException("Could not import the new route map.");
                app.authoredEnvironment = preview.transform; EditorUtility.SetDirty(app);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save the new scene.");
                Selection.activeGameObject = root.gameObject;
                Debug.Log("Added 24 proposed ground-floor rooms in six other buildings. CSE preserved. Scene: " +
                    output + ". Press Play and select a new building/room. Device testing is still required.");
                EditorUtility.DisplayDialog("Other-block rooms added",
                    "24 rooms added in a scene copy. CSE was retained.\n\nScene: " + output +
                    "\n\nPress Play and choose one of the new room destinations.", "OK");
            }
            catch (Exception error)
            {
                // Discard this operation's partial copy and return to the saved source.
                EditorSceneManager.OpenScene(sourcePath);
                AssetDatabase.DeleteAsset(output);
                if (File.Exists(json)) AssetDatabase.DeleteAsset(json);
                throw new InvalidOperationException("Other-block setup stopped; your source scene was retained. " + error.Message, error);
            }
        }

        static T Find<T>(Scene scene) where T : Component
        {
            foreach (var root in scene.GetRootGameObjects())
            { var item = root.GetComponentInChildren<T>(true); if (item != null) return item; }
            return null;
        }
        static bool SameSolid(CampusSolid a, CampusSolid b)
        {
            return a.name == b.name && a.floor == b.floor &&
                Vector3.Distance(new Vector3(a.x,a.y,a.z), new Vector3(b.x,b.y,b.z)) < .01f &&
                Vector3.Distance(new Vector3(a.sx,a.sy,a.sz), new Vector3(b.sx,b.sy,b.sz)) < .01f &&
                Mathf.Abs(a.rx-b.rx) < .01f;
        }
        static Transform Locate(Transform preview, CampusSolid solid)
        {
            var found = preview.GetComponentsInChildren<Transform>(true).Where(t =>
                t.name == solid.name && t.GetComponent<Renderer>() != null &&
                Vector3.Distance(preview.InverseTransformPoint(t.position), new Vector3(solid.x,solid.y,solid.z)) < .1f).ToArray();
            if (found.Length != 1) throw new InvalidOperationException("Cannot uniquely locate " + solid.name + ". Nothing changed in the source scene.");
            var item = found[0];
            // Do not delete user-added child objects or transformed custom building models.
            if (item.childCount != 0 || item.parent != preview ||
                Vector3.Distance(item.localScale, new Vector3(solid.sx,solid.sy,solid.sz)) > .01f ||
                Quaternion.Angle(item.localRotation, Quaternion.Euler(solid.rx,0,0)) > .1f)
                throw new InvalidOperationException(solid.name + " has custom geometry. No changes made; it needs fitting first.");
            return item;
        }
        static void HideOldDecoration(Transform preview, CampusSolid block)
        {
            var details = preview.Find("Campus Architectural Details");
            if (details == null) return;
            foreach (var renderer in details.GetComponentsInChildren<Renderer>(true))
            {
                var t = renderer.transform; var p = preview.InverseTransformPoint(t.position);
                if (Mathf.Abs(p.x-block.x) > block.sx/2+1.3f || Mathf.Abs(p.z-block.z) > block.sz/2+1.3f) continue;
                string n = t.name;
                bool known = n == block.name+" roof coping" || n == block.name+" roof inset" ||
                    n == block.name+" foundation" || n == block.name.ToUpperInvariant()+" sign" ||
                    n == block.name.ToUpperInvariant()+" lettering" || n.StartsWith("Window ") || n.StartsWith("Entry ");
                if (!known) continue;
                var role = t.GetComponent<CampusVisualRole>() ?? t.gameObject.AddComponent<CampusVisualRole>();
                role.replaced = true; renderer.enabled = false;
            }
        }
        static string CSESignature(Transform preview)
        {
            var records = new List<string>();
            foreach (var t in preview.GetComponentsInChildren<Transform>(true))
            {
                var p = preview.InverseTransformPoint(t.position);
                if (p.x < -16 || p.x > 16 || p.z < 25 || p.z > 45) continue;
                foreach (var component in t.GetComponents<Component>())
                    if (component != null) records.Add(component.GetInstanceID()+":"+EditorJsonUtility.ToJson(component));
            }
            records.Sort(StringComparer.Ordinal);
            return Hash128.Compute(string.Join("\n", records)).ToString();
        }
        static void ValidateConnected(NavigationGraphManager graph)
        {
            if (!graph.Nodes.ContainsKey("Entrance")) throw new InvalidOperationException("Campus entrance is missing.");
            foreach (var goal in graph.Nodes.Values)
                if (AStarPathfinder.Find(graph,"Entrance",goal.id).Count == 0)
                    throw new InvalidOperationException("Unreachable destination: " + goal.label);
        }
        static void ValidateNewRoutes(NavigationGraphManager graph, NavigationEdge[] edges, Transform preview)
        {
            foreach (var e in edges)
            {
                Vector3 a = preview.TransformPoint(graph.Nodes[e.a].Position + Vector3.up*.65f);
                Vector3 b = preview.TransformPoint(graph.Nodes[e.b].Position + Vector3.up*.65f);
                Vector3 delta = b-a;
                // A 0.25m swept radius checks actual scene obstacles as well as the data layout.
                foreach (var hit in Physics.OverlapSphere(a,.25f,1<<8,QueryTriggerInteraction.Ignore))
                    throw new InvalidOperationException("Route start overlaps " + hit.name + " at " + e.a);
                if (delta.magnitude > .001f && Physics.SphereCast(a,.25f,delta.normalized,out RaycastHit obstacle,delta.magnitude,1<<8,QueryTriggerInteraction.Ignore))
                    throw new InvalidOperationException("Route " + e.a + " to " + e.b + " is blocked by " + obstacle.collider.name);
            }
        }

        [MenuItem("Campus AR/Other Blocks/View Added Rooms")]
        public static void ViewRooms()
        {
            var preview = Find<ScenePreview>(SceneManager.GetActiveScene());
            var root = preview == null ? null : preview.transform.Find(RootName);
            if (root == null) { Debug.LogWarning("Open the AllBlocks scene first."); return; }
            Selection.activeGameObject = root.gameObject;
            if (SceneView.lastActiveSceneView != null)
                SceneView.lastActiveSceneView.LookAt(preview.transform.TransformPoint(new Vector3(0,0,3)), Quaternion.Euler(90,0,0), 80, true);
        }
    }
}
