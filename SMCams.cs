using Oxide.Core;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Secondary Monument Cameras", "RFC1920", "1.0.1")]
    [Description("Spawn extra server cameras for smaller monuments")]
    internal class SMCams : RustPlugin
    {
        private List<NetworkableId> networkableIds = new List<NetworkableId>();
        private static SortedDictionary<string, Vector3> monPos = new SortedDictionary<string, Vector3>();
        private static SortedDictionary<string, Vector3> monSize = new SortedDictionary<string, Vector3>();
        private static SortedDictionary<string, Vector3> cavePos = new SortedDictionary<string, Vector3>();

        //private const string camfab = "assets/prefabs/deployable/cctvcamera/cctv_deployed.prefab";
        private const string camfab = "assets/prefabs/deployable/ptz security camera/ptz_cctv_deployed.prefab";
        private readonly bool debug = false;

        private void OnServerInitialized()
        {
            FindMonuments();

            foreach (KeyValuePair<string, Vector3> moninfo in monPos)
            {
                SpawnCamera(moninfo.Key, moninfo.Value);
            }
        }
        private void DoLog(string message)
        {
            if (debug)
            {
                Interface.Oxide.LogInfo(message);
            }
        }

        private void SpawnCamera(string monument, Vector3 center)
        {
            string camid = monument.Replace(" ", "").ToUpper();
            Vector3 pos = new Vector3();
            switch (monument.Substring(0, 4).ToLower())
            {
                case "ware":
                    {
                        List<BaseEntity> ents = new List<BaseEntity>();
                        Vis.Entities(center, 25, ents);
                        foreach (BaseEntity entity in ents)
                        {
                            if (entity.PrefabName.Contains("repair"))
                            {
                                pos = Vector3.Lerp(center, entity.transform.position, 0.55f) + new Vector3(0, 5.7f, 0);
                                break;
                            }
                        }
                        DoLog($"{monument}: {center}");
                    }
                    break;
                case "ligh":
                    {
                        List<BaseEntity> ents = new List<BaseEntity>();
                        Vis.Entities(center, 100, ents);
                        foreach (BaseEntity entity in ents)
                        {
                            if (entity.PrefabName.Contains("recycle"))
                            {
                                pos = Vector3.Lerp(center, entity.transform.position, 0.22f) + new Vector3(0, 24f, 0);
                                break;
                            }
                        }
                        DoLog($"{monument}: {center}");
                    }
                    break;
                case "gas ":
                    {
                        List<BaseEntity> ents = new List<BaseEntity>();
                        Vis.Entities(center, 100, ents);
                        foreach (BaseEntity entity in ents)
                        {
                            if (entity.PrefabName.Contains("recycle"))
                            {
                                pos = Vector3.Lerp(center, entity.transform.position, 0.8f) + new Vector3(0, 4.4f, 0);
                                break;
                            }
                        }
                        DoLog($"{monument}: {center}");
                    }
                    break;
                case "supe":
                    {
                        List<BaseEntity> ents = new List<BaseEntity>();
                        Vis.Entities(center, 100, ents);
                        foreach (BaseEntity entity in ents)
                        {
                            if (entity.PrefabName.Contains("recycle"))
                            {
                                pos = Vector3.Lerp(center, entity.transform.position, 0.46f) + new Vector3(0, 3.5f, 0);
                                break;
                            }
                        }
                        DoLog($"{monument}: {center}");
                    }
                    break;
                default:
                    return;
            }
            CCTV_RC cam = (CCTV_RC)GameManager.server.CreateEntity(camfab, pos, new Quaternion(), true);
            cam.SetFlag(BaseEntity.Flags.Reserved8, true); // Power
            cam.SetFlag(BaseEntity.Flags.On, true, false, true);
            cam.SetFlag(BaseEntity.Flags.Locked, false);
            cam.isStatic = true;
            cam.hasPTZ = true;
            cam.UpdateIdentifier(camid, true);
            cam.Spawn();
            cam.SendNetworkUpdateImmediate();
            DoLog($"Spawned cam for {monument} at {pos} with id {cam.net.ID.Value}");
            DoLog($"Size: {monSize[monument]}");
            networkableIds.Add(cam.net.ID);
        }

        private void Unload()
        {
            foreach (NetworkableId id in networkableIds)
            {
                BaseNetworkable cam = BaseNetworkable.serverEntities.Find(id);
                if (ReferenceEquals(cam, null)) continue;
                DoLog($"Destroying camera with id {cam.net.ID.Value}");
                cam.Kill();
            }
        }

        private void FindMonuments()
        {
            foreach (MonumentInfo monument in UnityEngine.Object.FindObjectsOfType<MonumentInfo>())
            {
                if (monument.name.Contains("power_sub"))
                {
                    continue;
                }

                float realWidth = 0f;
                string name = null;

                if (monument.name == "OilrigAI")
                {
                    name = "Small Oilrig";
                    realWidth = 100f;
                }
                else if (monument.name == "OilrigAI2")
                {
                    name = "Large Oilrig";
                    realWidth = 200f;
                }
                else
                {
                    name = Regex.Match(monument.name, @"\w{6}\/(.+\/)(.+)\.(.+)").Groups[2].Value.Replace("_", " ").Replace(" 1", "").Titleize() + " 0";
                }
                if (monPos.ContainsKey(name))
                {
                    if (monPos[name] == monument.transform.position) continue;
                    string newname = name.Remove(name.Length - 1, 1) + "1";
                    if (monPos.ContainsKey(newname))
                    {
                        newname = name.Remove(name.Length - 1, 1) + "2";
                    }
                    if (monPos.ContainsKey(newname))
                    {
                        continue;
                    }
                    name = newname;
                }

                if (cavePos.ContainsKey(name))
                {
                    name += RandomString();
                }

                Vector3 extents = monument.Bounds.extents;
                if (realWidth > 0f)
                {
                    extents.z = realWidth;
                }

                if (monument.name.Contains("cave"))
                {
                    cavePos.Add(name, monument.transform.position);
                }
                else
                {
                    if (extents.z < 1)
                    {
                        extents.z = 50f;
                    }
                    monPos.Add(name.Trim(), monument.transform.position);
                    monSize.Add(name.Trim(), extents);
                    //DoLog($"Found monument {name} at {monument.transform.position.ToString()}");
                }
            }
        }

        private string RandomString()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
            List<char> charList = chars.ToList();

            string random = "";

            for (int i = 0; i <= UnityEngine.Random.Range(5, 10); i++)
            {
                random += charList[UnityEngine.Random.Range(0, charList.Count - 1)];
            }
            return random;
        }
    }
}
