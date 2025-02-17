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
        private List<Vector3> _mdtLocations;
        private string _helpText;
        private bool _mdtOpen;
        private int _inputKey;

        internal ClientMain() => Init();

        private void Init()
        {
            //Grab data from config.json
            string rawJSON = LoadResourceFile(GetCurrentResourceName(), "plugins/StationMDT/config.json");
            var json = JObject.Parse(rawJSON);

            _helpText = json["Help_Text"].ToString();
            _mdtLocations = JsonConvert.DeserializeObject<List<Vector3>>(json["MDT_Locations"].ToString());
            _inputKey = int.Parse(json["Input_Key"].ToString());

            _mdtOpen = false;

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
                if (Game.PlayerPed.Position.DistanceToSquared(location) <= 2f)
                {
                    foundMdt = true;
                    break;
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
