using System;
using System.IO;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

using BestHTTP.SocketIO;

namespace CocosSync
{

    [Serializable]
    class SyncNodeData
    {
        public String uuid;
        public String name;

        public Vector3 position;
        public Vector3 scale;
        public Vector3 eulerAngles;
        public Quaternion rotation;
        public bool needMerge = false;

        public List<string> children = new List<string>();
        public List<string> components = new List<string>();

        public List<SyncNodeData> childrenData = new List<SyncNodeData>();

        public string GetData()
        {
            foreach (var data in childrenData)
            {
                this.children.Add(data.GetData());
            }
            childrenData.Clear();
            return JsonUtility.ToJson(this);
        }
    }

    [Serializable]
    class SyncSceneData
    {
        public int nodeCount = 0;
        public int componentCount = 0;
        public List<string> children = new List<string>();
        public List<SyncNodeData> childrenData = new List<SyncNodeData>();

        public string assetBasePath = "";
        public string projectPath = "";
        public string exportBasePath = "Exported";
        public Dictionary<string, SyncAssetData> assetsMap = new Dictionary<string, SyncAssetData>();
        public List<string> assets = new List<string>();

        public string forceSyncAsset = "";

        public string GetData()
        {
            foreach (var data in childrenData)
            {
                this.children.Add(data.GetData());
            }
            childrenData.Clear();
            return JsonUtility.ToJson(this);
        }
    }


    class ReturnDetailData
    {
        public string uuid;
        public string path;

        public ReturnDetailData(string uuid, string path)
        {
            this.uuid = uuid;
            this.path = path;
        }
    }

    class CocosSyncTool : EditorWindow
    {

        static CocosSyncTool _Instance = null;
        public static CocosSyncTool Instance
        {
            get
            {
                if (_Instance == null)
                {
                    _Instance = EditorWindow.GetWindow<CocosSyncTool>();
                }
                return _Instance;
            }
        }



        public string ForceSyncAsset = "";
        public int MaxChildCount = 100000;

        static private SocketManager Manager;
        static string address = "http://127.0.0.1:8877/socket.io/";

        public static SyncSceneData sceneData = null;

        public string exportBasePath = "Exported";

        private List<IEnumerator<object>> process = new List<IEnumerator<object>>();

        private Dictionary<string, SyncNodeData> syncedNodes = new Dictionary<string, SyncNodeData>();

        private ReturnDetailData returnDetailData = null;
        private string toSyncSceneDataPath = "";

        public UnityEngine.Object[] selectObjects;

        [MenuItem("Cocos/Sync Tool")]
        static void Init()
        {
            EditorWindow.GetWindow<CocosSyncTool>().Show();
        }

        static void CheckSocket()
        {
            if (Manager == null || Manager.State == SocketManager.States.Closed)
            {
                Manager = new SocketManager(new Uri(address));

                Manager.Socket.On(SocketIOEventTypes.Connect, (s, p, a) =>
                {
                    Debug.Log("CocosSync Connected!");
                });

                Manager.Socket.On(SocketIOEventTypes.Disconnect, (s, p, a) =>
                {
                    Debug.Log("CocosSync Disconnected!");
                });

                // The argument will be an Error object.
                Manager.Socket.On(SocketIOEventTypes.Error, (socket, packet, args) =>
                {
                    Debug.LogError(string.Format("Error: {0}", args[0].ToString()));
                });

                Manager.Socket.On("get-asset-detail", (socket, packet, args) =>
                {
                    // Debug.Log("get-asset-detail : Receive message : " + DateTime.Now);
                    CocosSyncTool.Instance.process.Add(OnGetDetail(socket, packet, args));
                });
            }
        }

        static IEnumerator<object> OnGetDetail(Socket socket, Packet packet, params object[] args)
        {
            var uuid = args[0].ToString();
            if (CocosSyncTool.sceneData == null)
            {
                Manager.Socket.Emit("get-asset-detail", uuid, null);
                yield return null;
            }

            SyncAssetData asset = null;
            CocosSyncTool.sceneData.assetsMap.TryGetValue(uuid, out asset);

            // Debug.Log("get-asset-detail : Process Asset data : " + DateTime.Now);

            var detail = "";
            if (asset != null)
            {
                detail = asset.GetDetailData();
            }

            // var now = DateTime.Now;
            // Debug.Log("get-asset-detail : Finished process data : " + now);

            string tempPath = "Temp/CocosSync/get-asset-detail.json";
            if (!Directory.Exists("Temp/CocosSync"))
            {
                Directory.CreateDirectory("Temp/CocosSync");
            }
            StreamWriter writer = new StreamWriter(tempPath);
            writer.WriteLine(detail);
            writer.Close();

            CocosSyncTool.Instance.returnDetailData = new ReturnDetailData(uuid, tempPath);

            // Debug.Log("get-asset-detail : Finished send data: " + DateTime.Now + " : " + DateTime.Now.Subtract(now).Milliseconds.ToString() + "ms");
        }

        void Update()
        {
            for (var i = process.Count - 1; i >= 0; i--)
            {
                if (!process[i].MoveNext())
                {
                    process.RemoveAt(i);
                }
            }

            if (Manager.State == SocketManager.States.Opening || Manager.State == SocketManager.States.Reconnecting)
            {
                return;
            }

            if (returnDetailData != null)
            {
                Manager.Socket.Emit("get-asset-detail", returnDetailData.uuid, returnDetailData.path);
                returnDetailData = null;
            }

            if (toSyncSceneDataPath != "")
            {
                Manager.Socket.Emit("sync-datas-with-file", toSyncSceneDataPath);
                toSyncSceneDataPath = "";
            }
        }

        void OnGUI()
        {
            CheckSocket();

            if (Manager.State == SocketManager.States.Opening || Manager.State == SocketManager.States.Reconnecting)
            {
                GUILayout.Label("Connecting.");
                return;
            }

            var activeObject = Selection.activeObject;
            if (activeObject == null)
            {
                GUILayout.Label("Please select an object.");
                return;
            }

            if (Selection.activeTransform != null)
            {
                if (GUILayout.Button("SyncSelectNode"))
                {
                    SyncSelectNode();
                }

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("MaxChildCount");
                this.MaxChildCount = EditorGUILayout.IntField(this.MaxChildCount);
                EditorGUILayout.EndHorizontal();
            }

            if (activeObject is Texture || activeObject is Material)
            {
                if (GUILayout.Button("SyncSelectAsset"))
                {
                    SyncSelectAsset();
                }
            }

            if (Selection.objects.Length > 0) {
                if (GUILayout.Button("CopySelectObjects")) {
                    selectObjects = Selection.objects;
                }
            }

            if (selectObjects != null && selectObjects.Length > 0) {
                if (Selection.activeTransform != null) {
                    var motionsComponent = Selection.activeTransform.GetComponent<Motions>();
                    if (motionsComponent != null) {
                        if (GUILayout.Button("CopySelectObjectsToMotion")) {
                            for (var i = 0; i < selectObjects.Length; i++) {
                                var motion = selectObjects[i] as Motion;
                                if (motion) {
                                    motionsComponent.motions.Add(motion);
                                }
                            }
                        }
                    }
                }
            }

            // if (GUILayout.Button("SyncScene"))
            // {
            //     SyncScene();
            // }

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("ForceSyncAsset");
            this.ForceSyncAsset = EditorGUILayout.TextField(this.ForceSyncAsset);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("ExportBasePath");
            this.exportBasePath = EditorGUILayout.TextField(this.exportBasePath);
            EditorGUILayout.EndHorizontal();

        }


        String getGuid(Transform obj)
        {
            var provider = obj.GetComponent<GuidProvider>();
            if (!provider)
            {
                provider = obj.gameObject.AddComponent<GuidProvider>();
            }

            if (obj.name.Contains("@"))
            {
                obj.name = obj.name.Split('@')[0];
            }

            return provider.guid;
        }

        void SyncSelectAsset()
        {
            BeginSync();

            foreach (var obj in Selection.objects)
            {
                if (obj is Texture)
                {
                    SyncAssetData.GetAssetData<SyncTextureData>(obj);
                }
                else if (obj is Material)
                {
                    SyncAssetData.GetAssetData<SyncMaterialData>(obj);
                }
            }

            SyncAssets();
            EndSync();
        }

        public void SyncNode(Transform t)
        {
            SyncNodeData rootData = null;
            SyncNodeData data = null;

            Transform curr = t;
            while (curr)
            {
                SyncNodeData pdata = null;
                if (data == null)
                {
                    pdata = ExportNode(curr, true, true);
                    data = pdata;
                }
                else
                {
                    pdata = ExportNode(curr, true);
                }

                var lastData = rootData;
                rootData = pdata;
                if (lastData != null)
                {
                    if (!syncedNodes.ContainsKey(lastData.uuid))
                    {
                        syncedNodes.Add(lastData.uuid, lastData);
                        rootData.childrenData.Add(lastData);
                    }
                }

                curr = curr.parent;
            }

            if (!syncedNodes.ContainsKey(rootData.uuid))
            {
                syncedNodes.Add(rootData.uuid, rootData);
                sceneData.childrenData.Add(rootData);
            }
        }


        void SyncSelectNode()
        {
            BeginSync();

            SyncNode(Selection.activeTransform);

            SyncAssets();
            EndSync();
        }

        DateTime beginTime;
        void BeginSync()
        {
            syncedNodes.Clear();
            sceneData = new SyncSceneData();
            beginTime = DateTime.Now;
        }

        void SyncAssets()
        {
            // serialize assets
            foreach (var pair in sceneData.assetsMap)
            {
                var uuid = pair.Key;
                var asset = pair.Value;

                if (asset is SyncTextureData)
                {
                    CocosSyncTool.sceneData.assets.Insert(0, asset.GetData());
                }
                else
                {
                    CocosSyncTool.sceneData.assets.Add(asset.GetData());
                }
            }
        }

        void EndSync()
        {
            sceneData.assetBasePath = Application.dataPath;
            sceneData.projectPath = Path.Combine(Application.dataPath, "../");
            sceneData.exportBasePath = this.exportBasePath;
            sceneData.forceSyncAsset = this.ForceSyncAsset;

            // sceneData = null;

            var jsonData = sceneData.GetData();

            toSyncSceneDataPath = Path.Combine(sceneData.projectPath, "Temp/CocosSync/sync-scene-data.json");
            if (!Directory.Exists("Temp/CocosSync"))
            {
                Directory.CreateDirectory("Temp/CocosSync");
            }
            StreamWriter writer = new StreamWriter(toSyncSceneDataPath);
            writer.WriteLine(jsonData);
            writer.Close();

            Debug.Log("End Sync: " + DateTime.Now.Subtract(beginTime).Milliseconds.ToString());
        }

        SyncNodeData ExportNode(Transform t, bool syncComponent = false, bool syncChildren = false)
        {
            SyncNodeData data = null;

            var uuid = getGuid(t);
            if (syncedNodes.ContainsKey(uuid))
            {
                syncedNodes.TryGetValue(uuid, out data);
                return data;
            }

            data = new SyncNodeData();
            data.uuid = uuid;
            data.name = t.name;
            data.position = t.localPosition;
            data.scale = t.localScale;
            data.eulerAngles = t.localEulerAngles;
            data.rotation = t.localRotation;

            sceneData.nodeCount++;

            if (syncComponent)
            {
                foreach (var comp in t.GetComponents<Component>())
                {
                    if (comp as MonoBehaviour)
                    {
                        if (!(comp as MonoBehaviour).enabled)
                        {
                            continue;
                        }
                    }

                    SyncComponentData compData = null;
                    if (comp is Terrain)
                    {
                        compData = new SyncTerrainData();
                    }
                    else if (comp is MeshRenderer)
                    {
                        compData = new SyncMeshRendererData();
                    }
                    else if (comp is SkinnedMeshRenderer)
                    {
                        compData = new SyncSkinnedMeshRendererData();
                    }
                    else if (comp is InstanceObject)
                    {
                        data.needMerge = true;

                        compData = new SyncInstanceObjectData();
                    }
                    else if (comp is Light)
                    {
                        compData = new SyncLightData();
                    }
                    else if (comp is ReflectionProbe)
                    {
                        compData = new SyncReflectionProbeData();
                    }
                    else if (comp is Motions)
                    {
                        compData = new SyncAnimatorData();
                    }

                    if (compData != null)
                    {
                        compData.Sync(comp);

                        compData.uuid = comp.GetInstanceID().ToString();
                        sceneData.componentCount++;

                        data.components.Add(compData.GetData());
                    }
                }
            }

            if (syncChildren)
            {
                var maxChildCount = this.MaxChildCount;

                var group = t.GetComponent<LODGroup>();
                if (group)
                {
                    maxChildCount = 1;
                }

                var childCount = Math.Min(t.childCount, maxChildCount);
                for (var i = 0; i < childCount; i++)
                {
                    var c = t.GetChild(i);
                    if (!c.gameObject.activeInHierarchy)
                    {
                        continue;
                    }
                    var childData = ExportNode(c, syncComponent, syncChildren);
                    if (!syncedNodes.ContainsKey(childData.uuid))
                    {
                        syncedNodes.Add(childData.uuid, childData);
                        data.childrenData.Add(childData);
                    }
                }
            }

            return data;
        }


        void OnDestroy()
        {
            if (Manager != null)
            {
                Manager.Close();
                Manager = null;
            }
        }
    }

}