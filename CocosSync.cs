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
        public bool needMerge = false;

        // public List<SyncNodeData> children = new List<SyncNodeData>();
        public List<string> children = new List<string>();
        public List<string> components = new List<string>();
    }

    [Serializable]
    class SyncSceneData
    {
        public int nodeCount = 0;
        public int componentCount = 0;
        public List<SyncNodeData> children = new List<SyncNodeData>();

        public string assetBasePath = "";
        public string projectPath = "";
        public string exportBasePath = "Exported";
        public Dictionary<string, SyncAssetData> assetsMap = new Dictionary<string, SyncAssetData>();
        public List<string> assets = new List<string>();

        public string forceSyncAsset = "";
    }

    class CocosSyncTool : EditorWindow
    {
        public static CocosSyncTool Instance
        {
            get
            {
                return EditorWindow.GetWindow<CocosSyncTool>();
            }
        }

        public string ForceSyncAsset = "";
        public int MaxChildCount = 100000;

        static private SocketManager Manager;
        static string address = "http://127.0.0.1:8877/socket.io/";

        public static SyncSceneData sceneData = null;

        public string exportBasePath = "Exported";

        private List<IEnumerator<object>> process = new List<IEnumerator<object>>();


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

            for (var i = 0; i < 10; i++)
            {
                if (Manager.State == SocketManager.States.Closed)
                {
                    yield return new WaitForSeconds(0.5f);
                }
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

            Manager.Socket.Emit("get-asset-detail", uuid, tempPath);
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
        }

        void OnGUI()
        {
            CheckSocket();

            if (Manager.State == SocketManager.States.Opening || Manager.State == SocketManager.States.Reconnecting)
            {
                GUILayout.Label("Connecting.");
                return;
            }


            if (GUILayout.Button("SyncSelect"))
            {
                SyncSelect();
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

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("MaxChildCount");
            this.MaxChildCount = EditorGUILayout.IntField(this.MaxChildCount);
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

        void SyncSelect()
        {
            if (Selection.activeTransform == null)
            {
                return;
            }

            sceneData = new SyncSceneData();

            var now = DateTime.Now;

            SyncNodeData rootData = null;
            SyncNodeData data = null;

            Transform t = Selection.activeTransform;

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
                    rootData.children.Add(JsonUtility.ToJson(lastData));
                }

                curr = curr.parent;
            }

            sceneData.children.Add(rootData);

            sceneData.assetBasePath = Application.dataPath;
            sceneData.projectPath = Path.Combine(Application.dataPath, "../");
            sceneData.exportBasePath = this.exportBasePath;
            sceneData.forceSyncAsset = this.ForceSyncAsset;

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

            object jsonData = JsonUtility.ToJson(sceneData);
            Manager.Socket.Emit("sync-datas", jsonData);

            // sceneData = null;

            Debug.Log("End Sync: " + DateTime.Now.Subtract(now).Milliseconds.ToString());
        }

        SyncNodeData ExportNode(Transform t, bool syncComponent = false, bool syncChildren = false)
        {
            SyncNodeData data = new SyncNodeData();

            data.uuid = getGuid(t);
            data.name = t.name;
            data.position = t.localPosition;
            data.scale = t.localScale;
            data.eulerAngles = t.localEulerAngles;

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
                    else if (comp is Animator)
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
                    data.children.Add(JsonUtility.ToJson(childData));
                }
            }

            return data;
        }


        void SyncScene()
        {

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