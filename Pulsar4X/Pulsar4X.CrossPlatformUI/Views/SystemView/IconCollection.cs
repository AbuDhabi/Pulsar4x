using System.Collections.Generic;
using Pulsar4X.ECSLib;
using Eto.Drawing;
using System;
using Pulsar4X.ViewModel.SystemView;

namespace Pulsar4X.CrossPlatformUI.Views
{
    internal class IconCollection
    {
        //public List<IconBase> Icons { get; } = new List<IconBase>();
        public Dictionary<Guid,List<IconBase>> IconDict { get; private set;} = new Dictionary<Guid, List<IconBase>>();
		private SystemMap_DrawableVM _vm;
		private Camera2dv2 _camera;
        public IconCollection(SystemMap_DrawableVM vm)
        {
			_vm = vm;
        }

        public void Init(IEnumerable<Entity> entities, Camera2dv2 camera)
        {
            //Icons.Clear();
            IconDict.Clear();
			_camera = camera;
            foreach (var item in entities)
            {
				AddIcon(item);
            }
        }

		private void AddIcon(Entity forEntity)
		{
            Guid entityID = forEntity.Guid;
            if(!IconDict.ContainsKey(entityID))
                IconDict.Add(entityID, new List<IconBase>());
            
			if (forEntity.HasDataBlob<OrbitDB>() && forEntity.GetDataBlob<OrbitDB>().Parent != null)
			{
				OrbitRing ring = new OrbitRing(forEntity, _camera);
                IconDict[entityID].Add(ring);
			}
			if (forEntity.HasDataBlob<NameDB>())
                IconDict[entityID].Add(new TextIcon(forEntity, _camera));

			EntityIcon entIcon = new EntityIcon(forEntity, _camera);
            IconDict[entityID].Add(entIcon);
		}

        public void DrawMe(Graphics g)
        {
            Dictionary<Guid,List<IconBase>> newIconDict = new Dictionary<Guid, List<IconBase>>();

			foreach (var entityItem in _vm.GetIconableEntites()) 
			{
				if (IconDict.ContainsKey(entityItem.Guid))
				{
                    foreach (var item in IconDict[entityItem.Guid])
                    {
                        item.DrawMe(g);
                    }					
				}
				else
				{
					AddIcon(entityItem);
                    foreach (var item in IconDict[entityItem.Guid])
                    {
                        item.DrawMe(g);
                    }   
				}
                newIconDict.Add(entityItem.Guid, IconDict[entityItem.Guid]);
			}
            IconDict = newIconDict; //so destroyed entites wont hang around in the IconDict
        }
    }

    internal interface IconBase
    {
        //sets the size of the icons
        float Scale { get; set; }

        void DrawMe(Graphics g);
    }

}
