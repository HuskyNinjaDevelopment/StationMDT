using FivePD.API;
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
        private List<MDTStation> _mdtStations;
        private Dictionary<MDTStation, int> _hasCheckpoint;
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

            _mdtStations = new List<MDTStation>();

            _helpText = json["Help_Text"].ToString();

            foreach (var item in json["MDT_Locations"])
            {
                float x = float.Parse(item["X"].ToString());
                float y = float.Parse(item["Y"].ToString());
                float z = float.Parse(item["Z"].ToString());

                bool enabled = bool.Parse(item["Enabled"].ToString());

                _mdtStations.Add( new MDTStation(new Vector3(x, y, z), enabled) );
            }

            _inputKey = int.Parse(json["Input_Key"].ToString());

            r = int.Parse(json["Color"]["R"].ToString());
            g = int.Parse(json["Color"]["G"].ToString());
            b = int.Parse(json["Color"]["B"].ToString());
            a = int.Parse(json["Color"]["A"].ToString());

            _id = int.Parse(json["ID"].ToString());

            _height = float.Parse(json["Height"].ToString());
            _radius = float.Parse(json["Radius"].ToString());

            _mdtOpen = false;
            _hasCheckpoint = new Dictionary<MDTStation, int>();

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

            foreach(MDTStation station in _mdtStations) 
            {
                float distance = World.GetDistance(Game.PlayerPed.Position, station.Position);
                if(distance <= 15f)
                {
                    //Make Checkpoint
                    if(station.UseCheckpoint && !_hasCheckpoint.ContainsKey(station)) 
                    {
                        float ground = station.Position.Z;

                        while(!GetGroundZFor_3dCoord(station.Position.X, station.Position.Y, station.Position.Z, ref ground, true)) { await Delay(0); }
                        int chkpt = CreateCheckpoint(_id, station.Position.X, station.Position.Y, ground, station.Position.X, station.Position.Y, ground, _radius, r, g, b, a, 0);
                        SetCheckpointCylinderHeight(chkpt, _height, _height, _radius);

                        _hasCheckpoint.Add(station, chkpt);
                    }

                    if (distance <= 2f)
                    {
                        foundMdt = true;
                    }
                }
                else
                {
                    if(station.UseCheckpoint && _hasCheckpoint.ContainsKey(station))
                    {
                        int chkpt = _hasCheckpoint[station];
                        DeleteCheckpoint(chkpt);

                        _hasCheckpoint.Remove(station);
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

    public class MDTStation
    {
        public Vector3 Position { get; set; }
        public bool UseCheckpoint { get; set; }

        public MDTStation(Vector3 position, bool useCheckpoint)
        {
            Position = position;
            UseCheckpoint = useCheckpoint;
        }
    }
}
