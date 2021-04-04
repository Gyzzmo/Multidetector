using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
namespace MultiDetector
{
    public class MultiDetector : BaseScript
    {
        private string resourceName;
        private Scaleform scaleform;
        private static Vector3 portee = new Vector3(50.0f, 50.0f, 50.0f), porteeObj = new Vector3(10, 10, 10);
        private Dictionary<Model, string> pedsModels, objectsModels;
        private Dictionary<int, string> pedsVoices;
        private List<string> pedsBones, vehiclesBones;
        private Dictionary<string, string> clothesBones = new Dictionary<string, string>()
        {
            { "ears_1","BONETAG_R_CLAVICLE" },
            { "ears_2","BONETAG_L_CLAVICLE" },
            { "tshirt_1","BONETAG_SPINE1" },
            { "tshirt_2","BONETAG_SPINE" },
            { "torso_1","BONETAG_SPINE" },
            { "torso_2","BONETAG_SPINE_ROOT" },
            { "pants_1","BONETAG_R_CALF" },
            { "pants_2","BONETAG_L_CALF" },
            { "watches_1","BONETAG_R_PH_HAND" },
            { "watches_2","BONETAG_L_PH_HAND" },
            { "shoes_1","BONETAG_R_PH_FOOT" },
            { "shoes_2","BONETAG_L_PH_FOOT" },
            { "mask_1","BONETAG_HEAD" },
            { "mask_2","BONETAG_NECK" },
            { "bproof_1","BONETAG_SPINE1" },
            { "bproof_2","BONETAG_SPINE" },
            { "chain_1","BONETAG_SPINE2" },
            { "chain_2","BONETAG_SPINE2" },
            { "helmet_1","BONETAG_HEAD" },
            { "helmet_2","BONETAG_NECK" },
            { "glasses_1","BONETAG_HEAD" },
            { "glasses_2","BONETAG_NECK" },
            { "bags_1","BONETAG_R_FINGER31" },
            { "bags_2","BONETAG_L_FINGER31" },
            { "arms","BONETAG_SPINE" },
        };
        private Dictionary<string, int> variations = new Dictionary<string, int>()
        {
            { "ears_1", 2 },
            { "ears_2", 2 },
            { "tshirt_1", 8 },
            { "tshirt_2", 8 },
            { "torso_1", 11 },
            { "torso_2", 11 },
            { "pants_1", 4 },
            { "pants_2", 4 },
            { "watches_1", 6 },
            { "watches_2", 6 },
            { "shoes_1", 6 },
            { "shoes_2", 6 },
            { "mask_1", 1 },
            { "mask_2", 1 },
            { "bproof_1", 9 },
            { "bproof_2", 9 },
            { "chain_1", 7 },
            { "chain_2", 7 },
            { "helmet_1", 0 },
            { "helmet_2", 0 },
            { "glasses_1", 1 },
            { "glasses_2", 1 },
            { "bags_1", 5 },
            { "bags_2", 5 },
            { "arms", 3 },
        };
        private int affLimiter = 0, limiter = 10, clothesLimiter = 2, r = 255, g = 150, b = 150, a = 180, mr = 255, mg = 150, mb = 150, ma = 90;
        private float offsetSize = 0.3f, addValue, defAddValue = 0.0005f;
        private Prop[] propPool;
        private Vehicle[] vehPool;
        private float textSize;
        private Ped[] pedPool;
        private Vector3 vehBonesDecallage = new Vector3(0, 0, -1.5f), pedBonesDecallage = new Vector3(0, 0, -1.7f), offsetCoords = Vector3.Zero;
        private bool started, showOffsetRunning, showPedsRunning, showPlayersRunning, showObjectsRunning, showVehiclesRunning, showVehicleBonesRunning, showPedBonesRunning, showPedClothesRunning;
        private string afficherPedsEvent, afficherVehEvent, afficherPlayersEvent, afficherObjEvent, afficherBonesVehicleEvent, afficherBonesPedEvent, afficherOffsetEvent, afficherPedsClothesEvent;
        private List<int> noNetworked = new List<int>(), networked = new List<int>();
        public MultiDetector()
        {
            resourceName = GetCurrentResourceName();
            EventHandlers["onClientResourceStart"] += new Action<string>(ResourceStart);
            EventHandlers["onClientResourceStop"] += new Action<string>(ResourceSop);
        }

        static Predicate<Ped> NotPlayer()
        {
            return delegate (Ped ped)
            {
                return !IsPedAPlayer(ped.Handle);
            };
        }
        static Predicate<Ped> Player()
        {
            return delegate (Ped ped)
            {
                return IsPedAPlayer(ped.Handle);
            };
        }
        static Predicate<Entity> Prox()
        {
            return delegate (Entity ent)
            {
                return World.GetDistance(ent.Position, Game.PlayerPed.Position) < portee.X;
            };
        }
        static Predicate<Entity> ProxObj()
        {
            return delegate (Entity ent)
            {
                return World.GetDistance(ent.Position, Game.PlayerPed.Position) < porteeObj.X;
            };
        }
        private async Task Pools()
        {
            if (showPedsRunning || showPedClothesRunning)
                pedPool = World.GetAllPeds();
            await Delay(1000);
            if (showVehiclesRunning)
                vehPool = World.GetAllVehicles();
            await Delay(1000);
            if (showObjectsRunning)
                propPool = World.GetAllProps();
            await Delay(1000);
        }
        private void ResourceSop(string resname)
        {
            if (resname == resourceName)
            {
                this.UnregisterEvents();
            }
        }
        private void ResourceStart(string resname)
        {
            if (resname == resourceName)
            {
                addValue = defAddValue;
                started = false;
                r = configreader.GetConfigKeyValue(resourceName, "multidetector_text_r", 0, 85);
                g = configreader.GetConfigKeyValue(resourceName, "multidetector_text_g", 0, 145);
                b = configreader.GetConfigKeyValue(resourceName, "multidetector_text_b", 0, 198);
                a = configreader.GetConfigKeyValue(resourceName, "multidetector_text_a", 0, 100);
                mr = configreader.GetConfigKeyValue(resourceName, "multidetector_marker_r", 0, 85);
                mg = configreader.GetConfigKeyValue(resourceName, "multidetector_marker_g", 0, 145);
                mb = configreader.GetConfigKeyValue(resourceName, "multidetector_marker_b", 0, 198);
                ma = configreader.GetConfigKeyValue(resourceName, "multidetector_marker_a", 0, 100);
                int p = configreader.GetConfigKeyValue(resourceName, "multidetector_distance", 0, 50);
                portee = new Vector3(p, p, p);
                p = configreader.GetConfigKeyValue(resourceName, "multidetector_objects_distance", 0, 10);
                porteeObj = new Vector3(p, p, p);
                limiter = configreader.GetConfigKeyValue(resourceName, "multidetector_show_bones_limit", 0, 10);
                string afficherPedsCmd = configreader.GetConfigKeyValue(resourceName, "multidetector_show_peds_info_command", 0, "");
                string afficherVehCmd = configreader.GetConfigKeyValue(resourceName, "multidetector_show_vehicle_info_command", 0, "");
                string afficherPlayersCmd = configreader.GetConfigKeyValue(resourceName, "multidetector_show_players_info_command", 0, "");
                string afficherObjCmd = configreader.GetConfigKeyValue(resourceName, "multidetector_show_objects_info_command", 0, "");
                string afficherBonesVehicleCmd = configreader.GetConfigKeyValue(resourceName, "multidetector_show_vehicle_bones_command", 0, "");
                string afficherBonesPedCmd = configreader.GetConfigKeyValue(resourceName, "multidetector_show_ped_bones_command", 0, "");
                string afficherOffsetCmd = configreader.GetConfigKeyValue(resourceName, "multidetector_show_offset_command", 0, "");
                string afficherPedsCLothesCmd = configreader.GetConfigKeyValue(resourceName, "multidetector_show_peds_clothes_command", 0, "");
                afficherPedsEvent = configreader.GetConfigKeyValue(resourceName, "multidetector_show_peds_info_event", 0, "");
                afficherVehEvent = configreader.GetConfigKeyValue(resourceName, "multidetector_show_vehicle_info_event", 0, "");
                afficherPlayersEvent = configreader.GetConfigKeyValue(resourceName, "multidetector_show_players_info_event", 0, "");
                afficherObjEvent = configreader.GetConfigKeyValue(resourceName, "multidetector_show_objects_info_event", 0, "");
                afficherBonesVehicleEvent = configreader.GetConfigKeyValue(resourceName, "multidetector_show_vehicle_bones_event", 0, "");
                afficherBonesPedEvent = configreader.GetConfigKeyValue(resourceName, "multidetector_show_ped_bones_event", 0, "");
                afficherOffsetEvent = configreader.GetConfigKeyValue(resourceName, "multidetector_show_offset_event", 0, "");
                afficherPedsClothesEvent = configreader.GetConfigKeyValue(resourceName, "multidetector_show_peds_clothes_event", 0, "");
                if (afficherPedsCmd != "" || afficherPedsEvent != "")
                {
                    pedsModels = new Dictionary<Model, string>();
                    foreach (string line in new LineReader(() => new StringReader(LoadResourceFile(resourceName, "ped_models.txt"))))
                        try
                        {
                            pedsModels.Add(new Model(line), line);
                        }
                        catch (Exception) { Debug.WriteLine("Loading ped model " + line + " failed"); }

                    pedsVoices = new Dictionary<int, string>();
                    foreach (string line in new LineReader(() => new StringReader(LoadResourceFile(resourceName, "ped_voices.txt"))))
                        pedsVoices.Add(GetHashKey(line), line);

                    if (afficherPedsCmd != "")
                        RegisterCommand(afficherPedsCmd, new Action<int, List<object>, string>((source, args, raw) =>
                        {
                            if (IsAceAllowed("command." + afficherPedsCmd) && started)
                                ExecShowPed();
                        }), false);

                }
                if (afficherBonesPedCmd != "" || afficherBonesPedEvent != "")
                {
                    pedsBones = new List<string>();
                    foreach (string line in new LineReader(() => new StringReader(LoadResourceFile(resourceName, "ped_bones.txt"))))
                        pedsBones.Add(line);
                    if (afficherBonesPedCmd != "")
                        RegisterCommand(afficherBonesPedCmd, new Action<int, List<object>, string>((source, args, raw) =>
                        {
                            if (IsAceAllowed("command." + afficherBonesPedCmd) && started)
                                ExecShowBonesPeds();
                        }), false);

                }
                if (afficherVehCmd != "" || afficherVehEvent != "")
                {
                    if (afficherVehCmd != "")
                        RegisterCommand(afficherVehCmd, new Action<int, List<object>, string>((source, args, raw) =>
                        {
                            if (IsAceAllowed("command." + afficherVehCmd) && started)
                                ExecShowVehicles();
                        }), false);
                }
                if (afficherObjCmd != "" || afficherObjEvent != "")
                {
                    objectsModels = new Dictionary<Model, string>();
                    foreach (string line in new LineReader(() => new StringReader(LoadResourceFile(resourceName, "objects.txt"))))
                        try
                        {
                            objectsModels.Add(new Model(line), line);
                        }
                        catch (Exception) { Debug.WriteLine("Loading object " + line + " failed"); }
                    if (afficherObjCmd != "")
                        RegisterCommand(afficherObjCmd, new Action<int, List<object>, string>((source, args, raw) =>
                        {
                            if (IsAceAllowed("command." + afficherObjCmd) && started)
                                ExecShowObjects();
                        }), false);
                }
                if (afficherPlayersCmd != "")
                    RegisterCommand(afficherPlayersCmd, new Action<int, List<object>, string>((source, args, raw) =>
                    {
                        if (IsAceAllowed("command." + afficherPlayersCmd) && started)
                            ExecShowPlayers();
                    }), false);

                if (afficherBonesVehicleCmd != "")
                {
                    vehiclesBones = new List<string>();
                    foreach (string line in new LineReader(() => new StringReader(LoadResourceFile(resourceName, "vehicle_bones.txt"))))
                        vehiclesBones.Add(line);
                    RegisterCommand(afficherBonesVehicleCmd, new Action<int, List<object>, string>((source, args, raw) =>
                    {
                        if (IsAceAllowed("command." + afficherBonesVehicleCmd) && started)
                            ExecShowBonesVehicles();
                    }), false);
                }

                if (afficherOffsetCmd != "")
                    RegisterCommand(afficherOffsetCmd, new Action<int, List<object>, string>((source, args, raw) =>
                    {
                        if (IsAceAllowed("command." + afficherOffsetCmd) && started)
                            ExecShowOffset();
                    }), false);

                if (afficherPedsCLothesCmd != "")
                    RegisterCommand(afficherPedsCLothesCmd, new Action<int, List<object>, string>((source, args, raw) =>
                    {
                        if (IsAceAllowed("command." + afficherPedsCLothesCmd) && started)
                            ExecShowPedClothes();
                    }), false);

                this.RegisterEvents();
            }
        }
        private void UnregisterEvents()
        {
            started = false;
            this.EventHandlers.Remove(afficherPedsEvent);
            this.EventHandlers.Remove(afficherVehEvent);
            this.EventHandlers.Remove(afficherPlayersEvent);
            this.EventHandlers.Remove(afficherObjEvent);
            this.EventHandlers.Remove(afficherBonesVehicleEvent);
            this.EventHandlers.Remove(afficherBonesPedEvent);
            this.EventHandlers.Remove(afficherOffsetEvent);
        }
        private void RegisterEvents()
        {
            started = true;
            if (afficherPedsEvent != "")
                this.EventHandlers.Add(afficherPedsEvent, new Action(ExecShowPed));
            if (afficherBonesPedEvent != "")
                this.EventHandlers.Add(afficherBonesPedEvent, new Action(ExecShowBonesPeds));
            if (afficherVehEvent != "")
                this.EventHandlers.Add(afficherVehEvent, new Action(ExecShowVehicles));
            if (afficherObjEvent != "")
                this.EventHandlers.Add(afficherObjEvent, new Action(ExecShowObjects));
            if (afficherPlayersEvent != "")
                this.EventHandlers.Add(afficherPlayersEvent, new Action(ExecShowPlayers));
            if (afficherBonesVehicleEvent != "")
                this.EventHandlers.Add(afficherBonesVehicleEvent, new Action(ExecShowBonesVehicles));
            if (afficherOffsetEvent != "")
                this.EventHandlers.Add(afficherOffsetEvent, new Action(ExecShowOffset));
            if (afficherPedsClothesEvent != "")
                this.EventHandlers.Add(afficherPedsClothesEvent, new Action(ExecShowPedClothes));
        }

        private void ExecShowPlayers()
        {
            if (showPlayersRunning)
                Tick -= ShowPlayers;
            else
                Tick += ShowPlayers;
            showPlayersRunning = !showPlayersRunning;
        }
        private async void ExecShowVehicles()
        {
            if (showVehiclesRunning)
            {
                Tick -= ShowVehicles;
                noNetworked.Clear();
                networked.Clear();
                vehPool = null;
                if (!showPedBonesRunning && !showObjectsRunning && !showPedClothesRunning)
                    Tick -= Pools;
            }
            else
            {
                if (!showPedBonesRunning && !showObjectsRunning && !showPedClothesRunning)
                    Tick += Pools;

                Tick += ShowVehicles;
            }
            showVehiclesRunning = !showVehiclesRunning;
        }
        private async void ExecShowObjects()
        {
            if (showObjectsRunning)
            {
                Tick -= ShowObjects;
                noNetworked.Clear();
                networked.Clear();
                propPool = null;
                if (!showPedBonesRunning && !showVehiclesRunning && !showPedClothesRunning)
                    Tick -= Pools;
            }
            else {
                if (!showPedBonesRunning && !showVehiclesRunning && !showPedClothesRunning)
                    Tick += Pools;
                Tick += ShowObjects;
            }
            showObjectsRunning = !showObjectsRunning;
        }
        private void ExecShowBonesVehicles()
        {
            affLimiter = 0;
            if (showVehicleBonesRunning)
            {
                Tick -= ShowBonesVehicle;
                if (scaleform != null && scaleform.IsLoaded)
                    scaleform.Dispose();

                scaleform = null;
            }
            else
            {
                textSize = 0.1f;
                Tick += ShowBonesVehicle;
            }
            showVehicleBonesRunning = !showVehicleBonesRunning;
        }
        private void ExecShowBonesPeds()
        {
            affLimiter = 0;
            if (showPedBonesRunning)
            {
                Tick -= ShowBonesPed;
                if (scaleform != null && scaleform.IsLoaded)
                    scaleform.Dispose();
                scaleform = null;
            }
            else
            {
                textSize = 0.05f;
                Tick += ShowBonesPed;
            }
            showPedBonesRunning = !showPedBonesRunning;
        }
        private void ExecShowOffset()
        {
            if (showOffsetRunning)
            {
                Tick -= ShowOffset;
                Tick -= ControllOffset;
                if (scaleform != null && scaleform.IsLoaded)
                    scaleform.Dispose();

                scaleform = null;
            }
            else
            {
                textSize = 0.1f;
                Tick += ShowOffset;
                Tick += ControllOffset;
            }
            showOffsetRunning = !showOffsetRunning;
        }
        private async void ExecShowPed()
        {
            if (showPedsRunning)
            {
                Tick -= ShowPeds;
                noNetworked.Clear();
                networked.Clear();
                pedPool = null;
                if (!showVehiclesRunning && !showObjectsRunning && !showPedClothesRunning)
                    Tick -= Pools;
            }
            else {
                if (!showVehiclesRunning && !showObjectsRunning && !showPedClothesRunning)
                    Tick += Pools;
                Tick += ShowPeds;
            }
            showPedsRunning = !showPedsRunning;
        }
        private async void ExecShowPedClothes()
        {
            affLimiter = 0;
            if (showPedClothesRunning)
            {
                Tick -= ShowPedsClothes;
                noNetworked.Clear();
                networked.Clear();
                pedPool = null;
                if (!showVehiclesRunning && !showObjectsRunning && !showPedsRunning)
                    Tick -= Pools;
            }
            else
            {
                textSize = 0.08f;
                if (!showVehiclesRunning && !showObjectsRunning && !showPedsRunning)
                    Tick += Pools;
                Tick += ShowPedsClothes;
            }
            showPedClothesRunning = !showPedClothesRunning;
        }

        private int GetComponentVariation(Ped ent, string elem)
        {
            if (elem.EndsWith("1"))
                if (elem.Contains("ears") || elem.Contains("watches") || elem.Contains("helmet") || elem.Contains("glasses"))
                    return GetPedPropIndex(ent.Handle, variations[elem]);
                else
                    return GetPedDrawableVariation(ent.Handle, variations[elem]);
            else
                if (elem.Contains("ears") || elem.Contains("watches") || elem.Contains("helmet") || elem.Contains("glasses"))
                return GetPedPropTextureIndex(ent.Handle, variations[elem]);
            else
                return GetPedTextureVariation(ent.Handle, variations[elem]);
        }
        private async Task ShowPedsClothes()
        {
            while (pedPool == null)
                await Delay(500);

            var pool = pedPool;
            InvalidateIdleCam();
            await ShowLimiterSf(clothesBones.Count);
            pool.ToList().FindAll(Prox()).ForEach(async elem => {
                if (elem.IsNearEntity(Game.PlayerPed, portee) && IsEntityOnScreen(elem.Handle))
                {
                    int it = 0;
                    for (int i = affLimiter * clothesLimiter; i < (affLimiter * clothesLimiter + clothesLimiter < clothesBones.Count ? affLimiter * clothesLimiter + clothesLimiter : clothesBones.Count); i++)
                    {
                        int bone = GetEntityBoneIndexByName(elem.Handle, clothesBones.ElementAt(i).Value);
                        Vector3 coords = GetWorldPositionOfEntityBone(elem.Handle, bone);

                        if (coords != Vector3.Zero)
                        {
                            Draw3DText(coords + pedBonesDecallage + new Vector3(0,0,-0.15f*it), clothesBones.ElementAt(i).Key + ": " + GetComponentVariation(elem, clothesBones.ElementAt(i).Key), 213, 84, 230, 255, 4, textSize, textSize);
                            it++;
                        }
                    }
                }
            });
            ControlLimiter(clothesBones.Count);
        }

        private async Task ShowPeds()
        {
            while (pedPool == null)
                await Delay(500);
            if (networked.Count > 500)
                networked.Clear();
            if (noNetworked.Count > 500)
                noNetworked.Clear();
            var pool = pedPool;
            pool.ToList().FindAll(NotPlayer()).FindAll(Prox()).ForEach(async elem => {
                if (!noNetworked.Contains(elem.Handle) && !NetworkDoesEntityExistWithNetworkId(elem.Handle))
                {
                    if (networked.Contains(elem.Handle))
                    {
                        if (elem.IsNearEntity(Game.PlayerPed, portee) && IsEntityOnScreen(elem.Handle))
                        {
                            int hvoice = GetAmbientVoiceNameHash(elem.Handle);
                            Draw3DText(elem.Position + new Vector3(0f, 0f, (elem.Model.GetDimensions().Z / 2) - 1.5f),
                                "Ped model: " + (pedsModels.ContainsKey(elem.Model) ? pedsModels[elem.Model] : "Unknown") + " Hash: " + elem.Model.GetHashCode(),
                                r, g, b, a, 4, 0.1f, 0.1f);
                            Draw3DText(elem.Position + new Vector3(0f, 0f, (elem.Model.GetDimensions().Z / 2) - 1.5f - 0.2f),
                                "Ped voice: " + (pedsVoices.ContainsKey(hvoice) ? pedsVoices[hvoice] : "Unknown") + " Hash: " + hvoice,
                                r, g, b, a, 4, 0.1f, 0.1f);
                            Draw3DText(elem.Position + new Vector3(0f, 0f, (elem.Model.GetDimensions().Z / 2) - 1.5f - 0.4f),
                                "Local ID: " + elem.Handle + " Network ID: " + elem.NetworkId.ToString(),
                                r, g, b, a, 4, 0.1f, 0.1f);
                        }
                    }
                    else
                    {
                        if (NetworkDoesNetworkIdExist(elem.NetworkId))
                        {
                            if (!networked.Contains(elem.Handle))
                                networked.Add(elem.Handle);
                        }
                        else
                            noNetworked.Add(elem.Handle);
                    }
                }
            });
        }
        private async Task ShowVehicles()
        {
            while (vehPool == null)
                await Delay(500);
            if (networked.Count > 500)
                networked.Clear();
            if (noNetworked.Count > 500)
                noNetworked.Clear();
            var pool = vehPool;
            pool.ToList().FindAll(Prox()).ForEach(async elem => {
                if (!noNetworked.Contains(elem.Handle) && !NetworkDoesEntityExistWithNetworkId(elem.Handle))
                {
                    if (networked.Contains(elem.Handle))
                    {
                        if (elem.IsNearEntity(Game.PlayerPed, portee) && IsEntityOnScreen(elem.Handle))
                        {
                            Draw3DText(elem.Position + new Vector3(0f, 0f, (elem.Model.GetDimensions().Z / 2) - 1.5f),
                                "Vehicle model: " + Game.GetGXTEntry(elem.DisplayName) + "/"+ elem.DisplayName + " Hash: " + elem.Model.GetHashCode(),
                                r, g, b, a, 4, 0.1f, 0.1f);
                            Draw3DText(elem.Position + new Vector3(0f, 0f, (elem.Model.GetDimensions().Z / 2) - 1.5f - 0.2f),
                                "Plate: " + GetVehicleNumberPlateText(elem.Handle) + " Mission entity: " + IsEntityAMissionEntity(elem.Handle).ToString(),
                                r, g, b, a, 4, 0.1f, 0.1f);
                            Draw3DText(elem.Position + new Vector3(0f, 0f, (elem.Model.GetDimensions().Z / 2) - 1.5f - 0.4f),
                                "Local ID: " + elem.Handle + " Network ID: " + elem.NetworkId.ToString(),
                                r, g, b, a, 4, 0.1f, 0.1f);
                        }
                    }
                    else
                    {
                        if (NetworkDoesNetworkIdExist(elem.NetworkId))
                        {
                            if (!networked.Contains(elem.Handle))
                                networked.Add(elem.Handle);
                        }
                        else if (!noNetworked.Contains(elem.Handle))
                            noNetworked.Add(elem.Handle);
                    }
                }
            });
        }
        private async Task ShowPlayers()
        {
            World.GetAllPeds().ToList().FindAll(Prox()).FindAll(Player()).ForEach(async elem => {
                if (elem.IsNearEntity(Game.PlayerPed, portee) && IsEntityOnScreen(elem.Handle))
                {
                    var hvoice = GetAmbientVoiceNameHash(elem.Handle);
                    Draw3DText(elem.Position + new Vector3(0f, 0f, (elem.Model.GetDimensions().Z / 2)-1.5f),
                        "Player model: " + (pedsModels.ContainsKey(elem.Model) ? pedsModels[elem.Model] : "Unknown") + " Hash: " + elem.Model.GetHashCode(),
                        r, g, b, a, 4, 0.1f, 0.1f);
                    Draw3DText(elem.Position + new Vector3(0f, 0f, (elem.Model.GetDimensions().Z / 2) - 1.5f - 0.4f),
                        "Local ID: " + elem.Handle + " Network ID: " + GetPlayerServerId(NetworkGetPlayerIndexFromPed(elem.Handle)),
                        r, g, b, a, 4, 0.1f, 0.1f);
                }
            });
        }
        private async Task ShowObjects()
        {
            while (propPool == null)
                await Delay(500);
            if (networked.Count > 100)
                networked.Clear();
            if (noNetworked.Count > 100)
                noNetworked.Clear();
            var pool = propPool;
            pool.ToList().FindAll(ProxObj()).ForEach(async elem => {
                if (!noNetworked.Contains(elem.Handle) && !NetworkDoesEntityExistWithNetworkId(elem.Handle) ||
                noNetworked.Contains(elem.Handle) && NetworkDoesEntityExistWithNetworkId(elem.Handle))
                    if (NetworkDoesNetworkIdExist(elem.NetworkId))
                    {
                        if (!networked.Contains(elem.Handle))
                            networked.Add(elem.Handle);
                        if (noNetworked.Contains(elem.Handle))
                            noNetworked.Remove(elem.Handle);
                    }
                    else
                    {
                        if (!noNetworked.Contains(elem.Handle))
                            noNetworked.Add(elem.Handle);
                        if (networked.Contains(elem.Handle))
                            networked.Remove(elem.Handle);
                    }
                Draw3DText(elem.Position + new Vector3(0f, 0f, (elem.Model.GetDimensions().Z / 2) - 1.5f),
                "Object model: " + (objectsModels.ContainsKey(elem.Model) ? objectsModels[elem.Model] : "Unknown") + " Hash: " + elem.Model.GetHashCode(),
                r, g, b, a, 4, 0.032f, 0.052f);
                Draw3DText(elem.Position + new Vector3(0f, 0f, (elem.Model.GetDimensions().Z / 2) - 1.5f - 0.1f),
                    "Coordinates: " + elem.Position,
                    r, g, b, a, 4, 0.032f, 0.052f);
                Draw3DText(elem.Position + new Vector3(0f, 0f, (elem.Model.GetDimensions().Z / 2) - 1.5f - 0.2f),
                    "Local ID: " + elem.Handle + " Network ID: " + (!noNetworked.Contains(elem.Handle) ? elem.NetworkId.ToString():"Unknown"),
                    r, g, b, a, 4, 0.032f, 0.052f);
                float size = Math.Max(elem.Model.GetDimensions().X, elem.Model.GetDimensions().Y);
                World.DrawMarker((MarkerType)1, elem.Position, Vector3.Zero, Vector3.Zero, new Vector3(size, size, size), Color.FromArgb(ma, mr, mg, mb));
            });
        }
        private async Task ShowBonesVehicle()
        {
            if (Game.PlayerPed.IsSittingInVehicle())
            {
                InvalidateVehicleIdleCam();
                await ShowLimiterSf(vehiclesBones.Count);
                int vehicle = GetVehiclePedIsIn(Game.PlayerPed.Handle, false);
                for (int i = affLimiter * limiter; i < (affLimiter * limiter + limiter < vehiclesBones.Count ? affLimiter * limiter + limiter: vehiclesBones.Count); i++) {
                    var bone = GetEntityBoneIndexByName(vehicle, vehiclesBones[i]);
                    Vector3 coords = GetWorldPositionOfEntityBone(vehicle, bone);

                    if (coords != Vector3.Zero)
                    {
                        World.DrawMarker((MarkerType)28, coords, Vector3.Zero, Vector3.Zero, new Vector3(offsetSize, offsetSize, offsetSize), Color.FromArgb(120, mr, mg, mb));
                        Draw3DText(coords + vehBonesDecallage, vehiclesBones[i], 213, 84, 230, 255, 4, textSize, textSize);
					}
                }
                ControlLimiter(vehiclesBones.Count);
            }
        }
        private void Recalc(int total)
        {
            scaleform.CallFunction("CLEAR_ALL");
            scaleform.CallFunction("TOGGLE_MOUSE_BUTTONS", 0);
            scaleform.CallFunction("CREATE_CONTAINER");
            scaleform.CallFunction("SET_MAX_WIDTH", 1);
            scaleform.CallFunction("SET_BACKGROUND_COLOUR", r, g, b, a);
            scaleform.CallFunction("SET_DATA_SLOT", 0, GetControlInstructionalButton(2, 177, 0), "Quit");
            scaleform.CallFunction("SET_DATA_SLOT", 1, GetControlInstructionalButton(2, 14, 0), "Text size +");
            scaleform.CallFunction("SET_DATA_SLOT", 2, GetControlInstructionalButton(2, 16, 0), "Text size -");
            if (!showPedClothesRunning)
            {
                scaleform.CallFunction("SET_DATA_SLOT", 3, GetControlInstructionalButton(2, 175, 0), "Marker size +");
                scaleform.CallFunction("SET_DATA_SLOT", 4, GetControlInstructionalButton(2, 174, 0), "Marker size -");
            }
            scaleform.CallFunction("SET_DATA_SLOT", 5, GetControlInstructionalButton(2, 172, 0), "Next");
            if (!showPedClothesRunning)
                scaleform.CallFunction("SET_DATA_SLOT", 6, GetControlInstructionalButton(2, 173, 0), affLimiter * limiter + 1 + "-" + (affLimiter * limiter + limiter < total ? affLimiter * limiter + limiter : total) + "/" + total + "    Previous");
            else
                scaleform.CallFunction("SET_DATA_SLOT", 6, GetControlInstructionalButton(2, 173, 0), affLimiter * clothesLimiter + 1 + "-" + (affLimiter * clothesLimiter + clothesLimiter < total ? affLimiter * clothesLimiter + clothesLimiter : total) + "/" + total + "    Previous");
            scaleform.CallFunction("DRAW_INSTRUCTIONAL_BUTTONS", -1);
        }
        private async Task ShowLimiterSf(int total)
        {
            if (scaleform == null || !scaleform.IsLoaded)
            {
                scaleform = new Scaleform("instructional_buttons");
                while (!scaleform.IsLoaded)
                {
                    await Delay(50);
                }
                Recalc(total);
            }
            else
            {
                DrawScaleformMovieFullscreen(scaleform.Handle, 255 - r, 255 - g, 255 - b, 255 - a, 0);
            }
        }
        private async Task ShowBonesPed()
        {
            if (Game.PlayerPed.IsOnFoot)
            {
                InvalidateIdleCam();
                await ShowLimiterSf(pedsBones.Count);
                for (int i = affLimiter * limiter; i < (affLimiter * limiter+limiter < pedsBones.Count ? affLimiter * limiter + limiter : pedsBones.Count); i++)
                {
                    int bone = GetEntityBoneIndexByName(Game.PlayerPed.Handle, pedsBones[i]);
                    Vector3 coords = GetWorldPositionOfEntityBone(Game.PlayerPed.Handle, bone);

                    if (coords != Vector3.Zero)
                    {
                        World.DrawMarker((MarkerType)28, coords, Vector3.Zero, Vector3.Zero, new Vector3(offsetSize, offsetSize, offsetSize), Color.FromArgb(120, mr, mg, mb));
                        Draw3DText(coords + pedBonesDecallage, pedsBones[i], 213, 84, 230, 255, 4, textSize, textSize);
                    }
                }
                ControlLimiter(pedsBones.Count);
            }
        }
        private void ControlLimiter(int total)
        {
            DisableAllControls();
            if (IsDisabledControlJustPressed(0, 172)) 
            {
                affLimiter = affLimiter== total / (showPedClothesRunning ? clothesLimiter : limiter) ? 0:(affLimiter + 1) >= total/ (showPedClothesRunning ? clothesLimiter : limiter) ? total / (showPedClothesRunning ? clothesLimiter : limiter) : (affLimiter + 1);
                Recalc(total);
            }
            if (IsDisabledControlJustPressed(0, 173))
            {
                affLimiter = affLimiter - 1 >= 0 ? affLimiter - 1 : total / (showPedClothesRunning? clothesLimiter: limiter);
                Recalc(total);
            }
            if (!showPedClothesRunning)
            {
                if (IsDisabledControlPressed(0, 174))
                    offsetSize -= 0.0025f;
                if (IsDisabledControlPressed(0, 175))
                    offsetSize += 0.0025f;
            }
            if (IsDisabledControlPressed(0, 14) || IsDisabledControlPressed(0, 16))
                textSize -= 0.01f;
            if (IsDisabledControlPressed(0, 15) || IsDisabledControlPressed(0, 17))
                textSize += 0.01f;
            if (IsDisabledControlJustPressed(0, 177))
                if (showOffsetRunning)
                    ExecShowOffset();
                else if (showPedBonesRunning)
                    ExecShowBonesPeds();
                else if (showVehicleBonesRunning)
                    ExecShowBonesVehicles();
                else if (showPedClothesRunning)
                    ExecShowPedClothes();
        }
        private async Task ShowOffset()
        {
            if (scaleform == null || !scaleform.IsLoaded)
            {
                scaleform = new Scaleform("instructional_buttons");
                while (!scaleform.IsLoaded)
                {
                    await Delay(50);
                }
                if (scaleform.IsLoaded)
                {
                    scaleform.CallFunction("CLEAR_ALL");
                    scaleform.CallFunction("TOGGLE_MOUSE_BUTTONS", 0);
                    scaleform.CallFunction("CREATE_CONTAINER");
                    scaleform.CallFunction("SET_MAX_WIDTH", 1);
                    scaleform.CallFunction("SET_BACKGROUND_COLOUR", r, g, b, a);
                    scaleform.CallFunction("SET_DATA_SLOT", 0, GetControlInstructionalButton(2, 177, 0), "Quit");
                    scaleform.CallFunction("SET_DATA_SLOT", 1, GetControlInstructionalButton(2, 14, 0), "Text size +");
                    scaleform.CallFunction("SET_DATA_SLOT", 2, GetControlInstructionalButton(2, 16, 0), "Text size -");
                    scaleform.CallFunction("SET_DATA_SLOT", 3, GetControlInstructionalButton(2, 175, 0), "Marker size +");
                    scaleform.CallFunction("SET_DATA_SLOT", 4, GetControlInstructionalButton(2, 174, 0), "Marker size -");
                    scaleform.CallFunction("SET_DATA_SLOT", 5, GetControlInstructionalButton(2, 172, 0), "Z -");
                    scaleform.CallFunction("SET_DATA_SLOT", 6, GetControlInstructionalButton(2, 173, 0), "Z +");
                    scaleform.CallFunction("SET_DATA_SLOT", 7, GetControlInstructionalButton(2, 33, 0), "Y -");
                    scaleform.CallFunction("SET_DATA_SLOT", 8, GetControlInstructionalButton(2, 32, 0), "Y +");
                    scaleform.CallFunction("SET_DATA_SLOT", 9, GetControlInstructionalButton(2, 35, 0), "X -");
                    scaleform.CallFunction("SET_DATA_SLOT", 10, GetControlInstructionalButton(2, 34, 0), "X +");
                    scaleform.CallFunction("DRAW_INSTRUCTIONAL_BUTTONS", -1);
                }
            }
            else
                DrawScaleformMovieFullscreen(scaleform.Handle, 255 - r, 255 - g, 255 - b, 255 - a, 0);
            Vector3 pos = GetOffsetFromEntityInWorldCoords(Game.PlayerPed.IsSittingInVehicle()?GetVehiclePedIsIn(Game.PlayerPed.Handle, false):Game.PlayerPed.Handle, offsetCoords.X, offsetCoords.Y, offsetCoords.Z);
            World.DrawMarker((MarkerType)28, pos, Vector3.Zero, Vector3.Zero, new Vector3(offsetSize, offsetSize, offsetSize), Color.FromArgb(120, mr, mg, mb));
            Draw3DText(pos + new Vector3(0,0,-1.5f), "X: " + offsetCoords.X + " Y: " + offsetCoords.Y + " Z: "+ offsetCoords.Z, 213, 84, 230, 255, 4, textSize, textSize);
        }
        private async Task ControllOffset()
        {
            DisableAllControls();
            int m = 0;
            if (IsDisabledControlJustPressed(0, 172))
                while (IsDisabledControlPressed(0, 172))
                {
                    offsetCoords = offsetCoords + new Vector3(0, 0, addValue);
                    m++;
                    if ((m % 50) == 0)
                        addValue += addValue;

                    await Delay(0);
                }
            if (IsDisabledControlJustPressed(0, 173))
                while (IsDisabledControlPressed(0, 173))
                {
                    offsetCoords = offsetCoords + new Vector3(0, 0, -addValue);
                    m++;
                    if ((m % 50) == 0)
                        addValue += addValue;

                    await Delay(0);
                }
            if (IsDisabledControlPressed(0, 174))
                offsetSize -= 0.0025f;
            if (IsDisabledControlPressed(0, 175))
                offsetSize += 0.0025f;

            if (IsDisabledControlJustPressed(0, 32))
                while (IsDisabledControlPressed(0, 32))
                {
                    DisableAllControls();
                    offsetCoords = offsetCoords + new Vector3(0, addValue, 0);
                    m++;
                    if ((m % 50) == 0)
                        addValue += addValue;

                    await Delay(0);
                }
            if (IsDisabledControlJustPressed(0, 33))
                while (IsDisabledControlPressed(0, 33))
                {
                    DisableAllControls();
                    offsetCoords = offsetCoords + new Vector3(0, -addValue, 0);
                    m++;
                    if ((m % 50) == 0)
                        addValue += addValue;

                    await Delay(0);
                }
            if (IsDisabledControlJustPressed(0, 34))
                while (IsDisabledControlPressed(0, 34))
                {
                    DisableAllControls();
                    offsetCoords = offsetCoords + new Vector3(-addValue, 0, 0);
                    m++;
                    if ((m % 50) == 0)
                        addValue += addValue;

                    await Delay(0);
                }
            if (IsDisabledControlJustPressed(0, 35))
                while (IsDisabledControlPressed(0, 35))
                {
                    DisableAllControls();
                    offsetCoords = offsetCoords + new Vector3(addValue, 0, 0);
                    m++;
                    if ((m % 50) == 0)
                        addValue += addValue;

                    await Delay(0);
                }
            if (IsDisabledControlPressed(0, 14) || IsDisabledControlPressed(0, 16))
                textSize -= 0.01f;
            if (IsDisabledControlPressed(0, 15) || IsDisabledControlPressed(0, 17))
                textSize += 0.01f;
            if (IsDisabledControlJustPressed(0, 177))
                if (showOffsetRunning)
                    ExecShowOffset();
                else if (showPedBonesRunning)
                    ExecShowBonesPeds();
                else if (showVehicleBonesRunning)
                    ExecShowBonesVehicles();
            addValue = defAddValue;
        }

        private void DisableAllControls()
        {
            DisableAllControlActions(0);
            DisableAllControlActions(1);
            DisableAllControlActions(2);
            EnableControlAction(0, 0, true);
            EnableControlAction(0, 1, true);
            EnableControlAction(0, 2, true);
            if (showPedClothesRunning)
            {
                EnableControlAction(0, 32, true);
                EnableControlAction(0, 33, true);
                EnableControlAction(0, 34, true);
                EnableControlAction(0, 35, true);
                EnableControlAction(1, 32, true);
                EnableControlAction(1, 33, true);
                EnableControlAction(1, 34, true);
                EnableControlAction(1, 35, true);
                EnableControlAction(2, 32, true);
                EnableControlAction(2, 33, true);
                EnableControlAction(2, 34, true);
                EnableControlAction(2, 35, true);
            }
        }

        private void Draw3DText(Vector3 pos, string textInput, int r, int g, int b, int a, int fontId, float scaleX, float scaleY)
        {
            Vector3 cam = GetGameplayCamCoords();
            float dist = World.GetDistance(cam, pos);
            float scale = (1 / dist) * 20;
            float fov = (1 / GetGameplayCamFov()) * 100;
            scale = scale * fov;

            SetTextScale(scaleX * scale, scaleY * scale);
            SetTextFont(fontId);
            SetTextProportional(true);
            SetTextColour(r, g, b, a);
            SetTextDropshadow(2, 1, 1, 1, 255);
            SetTextEdge(3, 0, 0, 0, 150);
            SetTextDropShadow();
            SetTextOutline();
            SetTextEntry("STRING");
            SetTextCentre(true);
            AddTextComponentString(textInput);
            SetDrawOrigin(pos.X, pos.Y, pos.Z + 2.0f, 0);
            DrawText(0.0f, 0.0f);
            ClearDrawOrigin();
        }
        class configreader
        {
            public static T GetConfigKeyValue<T>(string resourceName, string metadataKey, int index, T defaultValue)
            {
                var result = defaultValue;
                try
                {
                    var input = GetResourceMetadata(resourceName, metadataKey, index);
                    result = (T)System.ComponentModel.TypeDescriptor.GetConverter(typeof(T)).ConvertFromString(input);
                }
                catch (Exception) {}
                return result;
            }
            public static T GetConfigKeyValue<T>(string resourceName, string metadataKey, int index)
            {
                return GetConfigKeyValue(resourceName, metadataKey, index, default(T));
            }
        }
    }
}
