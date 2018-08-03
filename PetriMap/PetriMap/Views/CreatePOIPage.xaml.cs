using System;
using System.Collections.Generic;

using Xamarin.Forms;
using Xamarin.Forms.GoogleMaps;

namespace PetriMap.Views
{
    public partial class CreatePOIPage : ContentPage
    {
        public CreatePOIPage()
        {
            InitializeComponent();
            Map.InitialCameraUpdate = CameraUpdateFactory.NewPositionZoom(new Position(49, 8), 13f);

        }

    }
}
