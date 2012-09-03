﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Pulsar4X.ViewModels;
using Pulsar4X.Entities;

namespace Pulsar4X.WinForms.Controls
{
    public partial class SystemGenAndDisplay : UserControl
    {
        public StarSystemViewModel VM { get; set; }
        public SystemGenAndDisplay()
        {
            InitializeComponent();

            VM = new StarSystemViewModel();

            NameListBox.DataSource = VM.StarSystems;
            NameListBox.Bind(c => c.SelectedItem, VM, d => d.CurrentStarSystem);
            NameListBox.DisplayMember = "Name";

            NameListBox.SelectedIndexChanged +=
                (s, args) => NameListBox.DataBindings["SelectedItem"].WriteValue();

            AgetextBox.Bind(c => c.Text, VM, d => d.CurrentStarSystemAge);

            // Setup the stars Grid
            StarsDataGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            StarsDataGridView.AutoGenerateColumns = false;
            StarsDataGridView.Bind(c => c.AllowUserToAddRows, VM, d => d.isSM);
            StarsDataGridView.Bind(c => c.AllowUserToDeleteRows, VM, d => d.isSM);
            StarsDataGridView.Bind(c => c.ReadOnly, VM, d => d.isNotSM);
            using (DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn())
            {
                col.DataPropertyName = "Name";
                col.HeaderText = "Name";
                StarsDataGridView.Columns.Add(col);
            }
            using (DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn())
            {
                col.DataPropertyName = "Class";
                col.HeaderText = "Class";
                StarsDataGridView.Columns.Add(col);
            }
            using (DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn())
            {
                col.DataPropertyName = "Mass";
                col.HeaderText = "Mass";
                StarsDataGridView.Columns.Add(col);
            }
            using (DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn())
            {
                col.DataPropertyName = "Luminosity";
                col.HeaderText = "Luminosity";
                StarsDataGridView.Columns.Add(col);
            }
            StarsDataGridView.DataSource = VM.StarsSource;
            StarsDataGridView.SelectionChanged += new EventHandler(StarsDataGridView_SelectionChanged);

            // Setup the starsA Grid
            PlanetsDataGridView.AutoGenerateColumns = false;
            PlanetsDataGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            PlanetsDataGridView.Bind(c => c.AllowUserToAddRows, VM, d => d.isSM);
            PlanetsDataGridView.Bind(c => c.AllowUserToDeleteRows, VM, d => d.isSM);
            PlanetsDataGridView.Bind(c => c.ReadOnly, VM, d => d.isNotSM);
            using (DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn())
            {
                col.DataPropertyName = "Name";
                col.HeaderText = "Name";
                PlanetsDataGridView.Columns.Add(col);
            }
            using (DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn())
            {
                col.DataPropertyName = "PlanetType";
                col.HeaderText = "Type";
                PlanetsDataGridView.Columns.Add(col);
            }
            using (DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn())
            {
                col.DataPropertyName = "MassInEarthMasses";
                col.HeaderText = "Mass";
                PlanetsDataGridView.Columns.Add(col);
            }
            using (DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn())
            {
                col.DataPropertyName = "MassOfGasInEarthMasses";
                col.HeaderText = "GasMass";
                PlanetsDataGridView.Columns.Add(col);
            }
            using (DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn())
            {
                col.DataPropertyName = "SurfaceGravity";
                col.HeaderText = "Gravity";
                PlanetsDataGridView.Columns.Add(col);
            }
            PlanetsDataGridView.DataSource = VM.PlanetSource;
            PlanetsDataGridView.SelectionChanged += new EventHandler(StarADataGridView_SelectionChanged);
        }

        void StarADataGridView_SelectionChanged(object sender, EventArgs e)
        {
            var sel = PlanetsDataGridView.SelectedRows;
            if (sel.Count > 0)
            {
                VM.CurrentPlanet = (Planet)sel[0].DataBoundItem;
            }
        }

        void StarsDataGridView_SelectionChanged(object sender, EventArgs e)
        {
            var sel = StarsDataGridView.SelectedRows;
            if (sel.Count > 0)
            {
                VM.CurrentStar = (Star)sel[0].DataBoundItem;
            }
        }

        private void SystemGenAndDisplay_Load(object sender, EventArgs e)
        {
            this.Dock = DockStyle.Fill;
        }

    }
}