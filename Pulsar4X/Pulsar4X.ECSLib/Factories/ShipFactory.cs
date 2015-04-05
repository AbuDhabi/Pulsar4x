﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pulsar4X.ECSLib.DataBlobs;

namespace Pulsar4X.ECSLib.Factories
{
    internal static class ShipFactory
    {
        private static int CreateShip(Guid classDef, EntityManager systemEM, int parentFormation)
        {
            throw new NotImplementedException();
        }

        private static int CreateNewShipClass(int factionID, string className = null)
        {
            // lets start by creating all the Datablobs that make up a ship class:
            var shipInfo = new ShipInfoDB();
            var armor = new ArmorDB();
            var beamWeapons = new BeamWeaponsDB();
            var buildCost = new BuildCostDB();
            var cargo = new CargoDB();
            var crew = new CrewDB();
            var damage = new DamageDB();
            var hanger = new HangerDB();
            var industry = new IndustryDB();
            var maintenance = new MaintenanceDB();
            var missileWeapons = new MissileWeaponsDB();
            var power = new PowerDB();
            var propulsion = new PropulsionDB();
            var sensorProfile = new SensorProfileDB();
            var sensors = new SensorsDB();
            var shields = new ShieldsDB();
            var tractor = new TractorDB();
            var troopTransport = new TroopTransportDB();

            // now lets create a list of all these datablobs so we can create our new entity:
            List<BaseDataBlob> shipDBList = new List<BaseDataBlob>()
            {
                shipInfo,
                armor,
                beamWeapons,
                buildCost,
                cargo,
                crew,
                damage,
                hanger,
                industry,
                maintenance,
                missileWeapons,
                power,
                propulsion,
                sensorProfile,
                sensors,
                shields,
                tractor,
                troopTransport
            };

            // now lets create the ship class:
            int shipClassEntity = Game.Instance.GlobalManager.CreateEntity(shipDBList);
            // and add it to the faction:
            var faction = Game.Instance.GlobalManager.GetDataBlob<FactionDB>(factionID);
            faction.ShipClasses.Add(new DataBlobRef<ShipInfoDB>(shipInfo));

            // now lets set some ship info:
            if (string.IsNullOrEmpty(className))
            {
                ///< @todo source the class name from faction theme.
                className = "New Class"; // <- Hack for now.
            }
            shipInfo.ClassName = className;
            shipInfo.Name = className;
            shipInfo.ShipClassDefinition = Guid.Empty; // just make sure it is marked as a class and not a ship.

            // now lets add some components:
            ///< @todo Add ship components
            // -- basic armour of current faction tech level
            // -- mimium crew quaters defaulting to 3 months deployment time.
            // -- a bridge
            // -- an engineering space
            // -- a fuel tank
            
            // now update the ship system DBs to reflect the components:
            ///< @todo update ship to reflect added components
        }
    }
}