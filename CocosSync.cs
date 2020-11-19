﻿using System;
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

        public List<SyncNodeData> children = new List<SyncNodeData>();
        public List<string> components = new List<string>();
    }

    [Serializable]
    class SyncSceneData
    {
        public int nodeCount = 0;
        public int componentCount = 0;
        public List<SyncNodeData> children = new List<SyncNodeData>();

        public String assetBasePath = "";
        public Dictionary<string, Dictionary<string, SyncAssetData>> assetsMap = new Dictionary<string, Dictionary<string, SyncAssetData>>();
        public List<string> assets = new List<string>();

        public bool forceSyncAsset = false;
    }

    class CocosSyncTool : EditorWindow
    {
        public bool ForceSyncAsset = false;
        public int MaxChildCount = 100000;

        static private SocketManager Manager;
        static string address = "http://127.0.0.1:8877/socket.io/";

        public static SyncSceneData sceneData = null;


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

            this.ForceSyncAsset = GUILayout.Toggle(this.ForceSyncAsset, "Force Sync Asset");

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

                if (rootData != null)
                {
                    pdata.children.Add(rootData);
                }

                rootData = pdata;
                curr = curr.parent;
            }

            sceneData.children.Add(rootData);

            sceneData.assetBasePath = Application.dataPath;
            sceneData.forceSyncAsset = this.ForceSyncAsset;

            object jsonData = JsonUtility.ToJson(sceneData);
            Manager.Socket.Emit("sync-datas", jsonData);

            sceneData = null;

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
                    else if (comp is MergeStatics)
                    {
                        data.needMerge = true;

                        compData = new SyncMergeStaticsData();
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
                    data.children.Add(childData);
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