using System;
using System.Collections.Generic;
using Pulsar4X.ECSLib;
using System.Windows.Threading;

namespace Pulsar4X.ViewModel.SystemView
{
    public class SystemMap_DrawableVM : ViewModelBase
    {
		private GameVM _gamevm;
		private StarSystem _starSys;
        public List<Entity> IconableEntitys { get; } = new List<Entity>();
        public ManagerSubPulse SystemSubpulse { get; private set; }

        public void Initialise(GameVM gameVM, StarSystem starSys)
        {
			_gamevm = gameVM;
			_starSys = starSys;
            IconableEntitys.Clear();
            IconableEntitys.AddRange(starSys.SystemManager.GetAllEntitiesWithDataBlob<PositionDB>(gameVM.CurrentAuthToken));
            SystemSubpulse = starSys.SystemManager.ManagerSubpulses;
            starSys.SystemManager.GetAllEntitiesWithDataBlob<NewtonBalisticDB>(gameVM.CurrentAuthToken);

            OnPropertyChanged(nameof(IconableEntitys));
        }

		public List<Entity> GetIconableEntites()
		{
			return _starSys.SystemManager.GetAllEntitiesWithDataBlob<PositionDB> (_gamevm.CurrentAuthToken);

		}

    }




}
