﻿using Eto.Forms;
using Eto.Serialization.Xaml;
using Pulsar4X.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Pulsar4X.CrossPlatformUI.Views
{
    public class ColonyScreenView : Panel
    {

        protected ComboBox ColonySelection { get; set; }
        protected GridView FacDataGrid { get; set; }
        protected GridView PopDataGrid { get; set; }
        protected GridView MineralDeposits { get; set; }
        protected GridView MineralStockpile { get; set; }
        protected GridView RefinedMats { get; set; }
        protected CargoView ColonyCargo { get; set; }
        // Awaiting redesign of TechProcessor, and ResearchVM's     
        // protected ResearchAbilityView ResearchAbilityView { get; set; }

        private ColonyScreenVM _colonyScreenVM;
        private GameVM gameVM { get; set; }

        protected ColonyScreenView()
        {
            XamlReader.Load(this);
            ColonySelection.BindDataContext(c => c.DataStore, (DictionaryVM<Guid, string> m) => m.DisplayList);
            ColonySelection.SelectedIndexBinding.BindDataContext((DictionaryVM<Guid, string> m) => m.SelectedIndex);

            FacDataGrid.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<FacilityVM, string>(r => r.Name) }
            });

            PopDataGrid.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property((KeyValuePair<string, long> r) => r.Key) }
            });
            PopDataGrid.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property((KeyValuePair<string, long> r) => r.Value).Convert(r => r.ToString()) }
            });

            MineralDeposits.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<PlanetMineralInfoVM, string>(r => r.Mineral) }
            });
            MineralDeposits.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<PlanetMineralInfoVM, double>(r => r.Accessability).Convert(r => r.ToString()) }
            });
            MineralDeposits.Columns.Add(new GridColumn
            {
                //DataCell = new TextBoxCell { Binding = Binding.Property<PlanetMineralInfoVM, int>(r => r.Amount).Convert(r => r.ToString()) }
            });

            //MineralStockpile.Columns.Add(new GridColumn
            //{
            //    DataCell = new TextBoxCell { Binding = Binding.Property<RawMineralInfoVM, string>(r => r.Mineral) }
            //});
            //MineralStockpile.Columns.Add(new GridColumn
            //{
            //    DataCell = new TextBoxCell { Binding = Binding.Property<RawMineralInfoVM, int>(r => r.Amount).Convert(r => r.ToString()) }
            //});


            //RefinedMats.Columns.Add(new GridColumn
            //{
            //    DataCell = new TextBoxCell { Binding = Binding.Property<RefinedMatInfoVM, string>(r => r.Material) }
            //});
            //RefinedMats.Columns.Add(new GridColumn
            //{
            //    DataCell = new TextBoxCell { Binding = Binding.Property<RefinedMatInfoVM, int>(r => r.Amount).Convert(r => r.ToString()) }
            //});

        }

        public ColonyScreenView(GameVM gameVM) :this()
        {
            this.gameVM = gameVM;

            ColonySelection.DataContext = gameVM.Colonys;
            gameVM.Colonys.SelectionChangedEvent += SetViewModel;
            SetViewModel(0, 0);
        }


        private void SetViewModel(int oldSelection, int newSelection)
        {

            _colonyScreenVM = gameVM.SelectedColonyScreenVM;
            DataContext = gameVM.SelectedColonyScreenVM;

            FacDataGrid.DataStore = _colonyScreenVM.Facilities;

            PopDataGrid.DataStore = _colonyScreenVM.Species.Cast<object>();

            ColonyCargo.Initialise(_colonyScreenVM.ColonyCargo);
            
            //MineralDeposits.DataStore = _colonyScreenVM.PlanetMineralDepositVM.MineralDeposits.Values;
            //gameVM.SelectedColonyScreenVM.PlanetMineralDepositVM.PropertyChanged += PlanetMineralDepositVM_PropertyChanged;

            //MineralStockpile.DataStore = _colonyScreenVM.RawMineralStockpileVM.MineralStockpile.Values;
            //gameVM.SelectedColonyScreenVM.RawMineralStockpileVM.PropertyChanged += RawMineralStockpileVM_PropertyChanged;
            
            //RefinedMats.DataStore = _colonyScreenVM.RefinedMatsStockpileVM.MaterialStockpile.Values;
            //gameVM.SelectedColonyScreenVM.RefinedMatsStockpileVM.PropertyChanged += RefinedMatsStockpileVM_PropertyChanged;
        }

        private void RefinedMatsStockpileVM_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            RefinedMats.DataStore = _colonyScreenVM.RefinedMatsStockpileVM.MaterialStockpile.Values;
        }

        private void RawMineralStockpileVM_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            MineralStockpile.DataStore = _colonyScreenVM.RawMineralStockpileVM.MineralStockpile.Values;
        }

        private void PlanetMineralDepositVM_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            MineralDeposits.DataStore = _colonyScreenVM.PlanetMineralDepositVM.MineralDeposits.Values;
        }
    }
}
