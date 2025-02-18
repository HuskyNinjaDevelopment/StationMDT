﻿using FivePD.API;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections;

namespace StationMDT
{
    internal class ClientMain: Plugin
    {
        private List<Vector3> _mdtLocations;
        private Dictionary<Vector3, int> _hasCheckpoint;
        private string _helpText;
        private bool _mdtOpen;
        private int _inputKey;
        private int r, b, g, a, _id;
        private float _height, _radius;

        internal ClientMain() => Init();

        private void Init()
        {
            //Grab data from config.json
            string rawJSON = LoadResourceFile(GetCurrentResourceName(), "plugins/StationMDT/config.json");
            var json = JObject.Parse(rawJSON);

            _helpText = json["Help_Text"].ToString();
            _mdtLocations = JsonConvert.DeserializeObject<List<Vector3>>(json["MDT_Locations"].ToString());
            _inputKey = int.Parse(json["Input_Key"].ToString());

            r = int.Parse(json["Color"]["R"].ToString());
            g = int.Parse(json["Color"]["G"].ToString());
            b = int.Parse(json["Color"]["B"].ToString());
            a = int.Parse(json["Color"]["A"].ToString());

            _id = int.Parse(json["ID"].ToString());

            _height = float.Parse(json["Height"].ToString());
            _radius = float.Parse(json["Radius"].ToString());

            _mdtOpen = false;
            _hasCheckpoint = new Dictionary<Vector3, int>();

            AddTextEntry("MDT:HELP_TEXT", _helpText);

            Tick += IsPlayerNearMDTLocation;
        }

        private async Task IsPlayerNearMDTLocation()
        {
            if (_mdtOpen)
            {
                await Delay(1000);
                return;
            }

            bool foundMdt = false;

            foreach(Vector3 location in _mdtLocations) 
            {
                float distance = World.GetDistance(Game.PlayerPed.Position, location);
                if(distance <= 15f)
                {
                    //Make Checkpoint
                    if(!_hasCheckpoint.ContainsKey(location)) 
                    {
                        float ground = location.Z;

                        while(!GetGroundZFor_3dCoord(location.X, location.Y, location.Z, ref ground, true)) { await Delay(0); }
                        int chkpt = CreateCheckpoint(_id, location.X, location.Y, ground, location.X, location.Y, ground, _radius, r, g, b, a, 0);
                        SetCheckpointCylinderHeight(chkpt, _height, _height, _radius);

                        _hasCheckpoint.Add(location, chkpt);
                    }

                    if (distance <= 2f)
                    {
                        foundMdt = true;
                    }
                }
                else
                {
                    if(_hasCheckpoint.ContainsKey(location))
                    {
                        int chkpt = _hasCheckpoint[location];
                        DeleteCheckpoint(chkpt);

                        _hasCheckpoint.Remove(location);
                    }
                }

            }

            if(foundMdt)
            {
                DisplayHelpTextThisFrame("MDT:HELP_TEXT", false);
                if(Game.IsControlJustPressed(0, (Control)_inputKey))
                {
                    SendNuiMessage("{\"type\":\"FIVEPD::Computer::UI\",\"display\":true}");
                    SetNuiFocus(true, true);
                    _mdtOpen = true;
                }
            }
            else
            {
                await Delay(500);
            }

            await Task.FromResult(0);
        }

        [EventHandler("__cfx_nui:exitComputerMenu")]
        private void HandleMDTExit()
        {
            _mdtOpen = false;
            SetNuiFocus(false, false);
        }
    }
}
