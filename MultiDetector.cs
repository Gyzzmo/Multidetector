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
        private int affLimiter = 0, limiter = 10, r = 255, g = 150, b = 150, a = 180, mr = 255, mg = 150, mb = 150, ma = 90;
        private float offsetSize = 0.3f, addValue, defAddValue = 0.0005f;
        private Prop[] propPool;
        private Vehicle[] vehPool;
        private float textSize;
        private Ped[] pedPool;
        private Vector3 vehBonesDecallage = new Vector3(0, 0, -1.5f), pedBonesDecallage = new Vector3(0, 0, -1.7f), offsetCoords = Vector3.Zero;
        private bool started, showOffsetRunning, showPedsRunning, showPlayersRunning, showObjectsRunning, showVehiclesRunning, showVehicleBonesRunning, showPedBonesRunning;
        private List<int> noNetworked = new List<int>(), networked = new List<int>();
        public MultiDetector()
        {
            resourceName = GetCurrentResourceName();
            EventHandlers["onClientResourceStart"] += new Action<string>(ResourceStart);
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
            if (showPedsRunning)
                pedPool = World.GetAllPeds();
            await Delay(1000);
            if (showVehiclesRunning)
                vehPool = World.GetAllVehicles();
            await Delay(1000);
            if (showObjectsRunning)
                propPool = World.GetAllProps();
            await Delay(1000);
        }
        private void ResourceStart(string resname)
        {
            if (resname == resourceName)
            {
                addValue = defAddValue;
                started = false;
                r = GetConfigKeyValue(resourceName, "multidetector_text_r", 0, 85);
                g = GetConfigKeyValue(resourceName, "multidetector_text_g", 0, 145);
                b = GetConfigKeyValue(resourceName, "multidetector_text_b", 0, 198);
                a = GetConfigKeyValue(resourceName, "multidetector_text_a", 0, 100);
                mr = GetConfigKeyValue(resourceName, "multidetector_marker_r", 0, 85);
                mg = GetConfigKeyValue(resourceName, "multidetector_marker_g", 0, 145);
                mb = GetConfigKeyValue(resourceName, "multidetector_marker_b", 0, 198);
                ma = GetConfigKeyValue(resourceName, "multidetector_marker_a", 0, 100);
                int p = GetConfigKeyValue(resourceName, "multidetector_distance", 0, 50);
                portee = new Vector3(p, p, p);
                p = GetConfigKeyValue(resourceName, "multidetector_objects_distance", 0, 10);
                porteeObj = new Vector3(p, p, p);
                limiter = GetConfigKeyValue(resourceName, "multidetector_show_bones_limit", 0, 10);
                string afficherPedsCmd = GetConfigKeyValue(resourceName, "multidetector_show_peds_info_command", 0, "");
                string afficherVehCmd = GetConfigKeyValue(resourceName, "multidetector_show_vehicle_info_command", 0, "");
                string afficherPlayersCmd = GetConfigKeyValue(resourceName, "multidetector_show_players_info_command", 0, "");
                string afficherObjCmd = GetConfigKeyValue(resourceName, "multidetector_show_objects_info_command", 0, "");
                string afficherBonesVehicleCmd = GetConfigKeyValue(resourceName, "multidetector_show_vehicle_bones_command", 0, "");
                string afficherBonesPedCmd = GetConfigKeyValue(resourceName, "multidetector_show_ped_bones_command", 0, "");
                string afficherOffsetCmd = GetConfigKeyValue(resourceName, "multidetector_show_offset_command", 0, "");
                afficherPedsEvent = GetConfigKeyValue(resourceName, "multidetector_show_peds_info_event", 0, "");
                afficherVehEvent = GetConfigKeyValue(resourceName, "multidetector_show_vehicle_info_event", 0, "");
                afficherPlayersEvent = GetConfigKeyValue(resourceName, "multidetector_show_players_info_event", 0, "");
                afficherObjEvent = GetConfigKeyValue(resourceName, "multidetector_show_objects_info_event", 0, "");
                afficherBonesVehicleEvent = GetConfigKeyValue(resourceName, "multidetector_show_vehicle_bones_event", 0, "");
                afficherBonesPedEvent = GetConfigKeyValue(resourceName, "multidetector_show_ped_bones_event", 0, "");
                afficherOffsetEvent = GetConfigKeyValue(resourceName, "multidetector_show_offset_event", 0, "");
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
                            {
                                ExecShowPed();
                            }
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
                            {
                                ExecShowBonesPeds();
                            }
                        }), false);

                }
                if (afficherVehCmd != "" || afficherVehEvent != "")
                {
                    if (afficherVehCmd != "")
                        RegisterCommand(afficherVehCmd, new Action<int, List<object>, string>((source, args, raw) =>
                        {
                            if (IsAceAllowed("command." + afficherVehCmd) && started)
                            {
                                ExecShowVehicles();
                            }
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
                            {
                                ExecShowObjects();
                            }
                        }), false);
                }
                if (afficherPlayersCmd != "")
                    RegisterCommand(afficherPlayersCmd, new Action<int, List<object>, string>((source, args, raw) =>
                    {
                        if (IsAceAllowed("command." + afficherPlayersCmd) && started)
                        {
                            ExecShowPlayers();
                        }
                    }), false);

                if (afficherBonesVehicleCmd != "")
                {
                    vehiclesBones = new List<string>();
                    foreach (string line in new LineReader(() => new StringReader(LoadResourceFile(resourceName, "vehicle_bones.txt"))))
                        vehiclesBones.Add(line);
                    RegisterCommand(afficherBonesVehicleCmd, new Action<int, List<object>, string>((source, args, raw) =>
                    {
                        if (IsAceAllowed("command." + afficherBonesVehicleCmd) && started)
                        {
                            ExecShowBonesVehicles();
                        }
                    }), false);
                }

                if (afficherOffsetCmd != "")
                    RegisterCommand(afficherOffsetCmd, new Action<int, List<object>, string>((source, args, raw) =>
                    {
                        if (IsAceAllowed("command." + afficherOffsetCmd) && started)
                        {
                            ExecShowOffset();
                        }
                    }), false);
                this.RegisterEvents();
            }
        }

        private string afficherPedsEvent, afficherVehEvent, afficherPlayersEvent, afficherObjEvent, afficherBonesVehicleEvent, afficherBonesPedEvent, afficherOffsetEvent;
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
                if (!showPedBonesRunning && !showObjectsRunning)
                    Tick -= Pools;
            }
            else
            {
                if (!showPedBonesRunning && !showObjectsRunning)
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
                if (!showPedBonesRunning && !showVehiclesRunning)
                    Tick -= Pools;
            }
            else {
                if (!showPedBonesRunning && !showVehiclesRunning)
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
                if (!showVehiclesRunning && !showObjectsRunning)
                    Tick -= Pools;
            }
            else {
                if (!showVehiclesRunning && !showObjectsRunning)
                    Tick += Pools;
                Tick += ShowPeds;
            }
            showPedsRunning = !showPedsRunning;
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
            scaleform.CallFunction("SET_DATA_SLOT", 3, GetControlInstructionalButton(2, 175, 0), "Marker size +");
            scaleform.CallFunction("SET_DATA_SLOT", 4, GetControlInstructionalButton(2, 174, 0), "Marker size -");
            scaleform.CallFunction("SET_DATA_SLOT", 5, GetControlInstructionalButton(2, 172, 0), "Next");
            scaleform.CallFunction("SET_DATA_SLOT", 6, GetControlInstructionalButton(2, 173, 0), affLimiter * limiter + 1 + "-" + (affLimiter * limiter + limiter < total ? affLimiter * limiter + limiter : total) + "/" + total + "    Previous");
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
                affLimiter = affLimiter== total / limiter ? 0:(affLimiter + 1) >= total/limiter ? total / limiter : (affLimiter + 1);
                Recalc(total);
            }
            if (IsDisabledControlJustPressed(0, 173))
            {
                affLimiter = affLimiter - 1 >= 0 ? affLimiter - 1 : total / limiter;
                Recalc(total);
            }
            if (IsDisabledControlPressed(0, 174))
                offsetSize -= 0.0025f;
            if (IsDisabledControlPressed(0, 175))
                offsetSize += 0.0025f;
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
