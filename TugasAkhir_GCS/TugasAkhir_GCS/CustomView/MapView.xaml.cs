﻿using BruTile.Cache;
using BruTile.Predefined;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Projection;
using Mapsui.UI;
using Mapsui.UI.Forms;
using Mapsui.UI.Forms.Extensions;
using Mapsui.UI.Objects;
using Mapsui.Utilities;
using Mapsui.Widgets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Animation = Xamarin.Forms.Animation;

namespace TugasAkhir_GCS.CustomView
{
    public partial class MapView : ContentView
    {
        public Pin Wahana, Home;

        public MapView()
        {
            InitializeComponent();

            mapView.MyLocationLayer.Enabled = false;
            mapView.RotationLock = false;
            mapView.UseDoubleTap = true;

            mapView.Map.CRS = "EPSG:3857";
            mapView.Map.Transformation = new MinimalTransformation();
            mapView.Map.Layers.Add(new TileLayer(
                tileSource: KnownTileSources.Create(
                    KnownTileSource.BingAerial,
                    Variables.BingMapsAPIKey,
                    new FileCache(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
                        + @"\MapCache\", "mdat")
                    )
                ));
            mapView.Map.Limiter = new ViewportLimiterKeepWithin
            {
                PanLimits = new Mapsui.Geometries.BoundingBox(
                    SphericalMercator.FromLonLat(-180, -85.06),
                    SphericalMercator.FromLonLat(180, 85.06))
            };
            mapView.Map.Widgets.Add(new Mapsui.Widgets.ScaleBar.ScaleBarWidget(mapView.Map)
            {
                TextAlignment = Alignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                MarginY = 30,
            });

            mapView.Map.Home = x => x.NavigateTo(new Mapsui.Geometries.BoundingBox(
                SphericalMercator.FromLonLat(lat: -11.00750, lon: 94.97278),
                SphericalMercator.FromLonLat(lat: 6.07500, lon: 141.01944)
            ), ScaleMethod.Fit);
        }

        public void UpdateGPS(int lat, int lon, int alt)
        {
            if (Wahana == null)
            {
                Wahana = new Pin(mapView)
                {
                    Label = "Wahana",
                    Type = PinType.Icon,
                    Icon = GetBytesFromResource("TugasAkhir_GCS.Resources.Images.quadcopter.png"),
                    Scale = 0.1f,
                    RotateWithMap = true,
                };
                mapView.Pins.Add(Wahana);

                Wahana.Position = new Position(lat * 0.0000001, lon * 0.0000001);

                mapView.Navigator.NavigateTo(
                    SphericalMercator.FromLonLat(Wahana.Position.Longitude, Wahana.Position.Latitude),
                    18.5.ToMapsuiResolution(), 3000, Mapsui.Utilities.Easing.CubicInOut);

                Home = new Pin(mapView)
                {
                    Label = "Home",
                    Type = PinType.Icon,
                    Icon = GetBytesFromResource("TugasAkhir_GCS.Resources.Images.home-ico.png"),
                    Scale = 0.05f,
                    RotateWithMap = false,
                };
                mapView.Pins.Add(Home);

                Home.Position = new Position(lat * 0.0000001, lon * 0.0000001);
                (Application.Current as App).ReturnTime.Home = new MavLinkNet.UasGlobalPositionInt() { Lat = lat, Lon = lon, RelativeAlt = alt };

                return;
            }
#if DATA_FETCH
            Wahana.Position = new Position(lat * 0.0000001, lon * 0.0000001);
#else
            (Application.Current as App).ReturnTime.UAV = new MavLinkNet.UasGlobalPositionInt() { Lat = lat, Lon = lon, RelativeAlt = alt };

            var asal_pos = Wahana.Position;
            var temp_pos = new Position(lat * 0.0000001, lon * 0.0000001);

            this.Animate(name: "PositionAnim", length: Application.Current.Resources["AnimLength"] as OnIdiom<byte>,
                transform: (time) =>
                {
                    var lat_interpolated = asal_pos.Latitude
                                        + (temp_pos.Latitude - asal_pos.Latitude)
                                        * time;

                    var lon_interpolated = asal_pos.Longitude
                                        + (temp_pos.Longitude - asal_pos.Longitude)
                                        * time;

                    return new Position(lat_interpolated, lon_interpolated);
                },
                callback: (val) => Wahana.Position = val/*,
                finished: (endpos, finished) => {
                    Wahana.Position = endpos;
                }*/);
#endif
        }

        internal byte[] GetBytesFromResource(string path)
        {
            return typeof(App).Assembly.GetManifestResourceStream(path).ToBytes();
        }

        public void UpdateBearing(float bearing)
        {
            if (Wahana == null)
                return;

            var hdg = bearing;
            if (bearing < Wahana.Rotation - 180)
                hdg += 360;
#if DATA_FETCH
            Wahana.Rotation = bearing;
#else
            new Animation(start: Wahana.Rotation, end: hdg,
                callback: val => Wahana.Rotation = (float)val,
                finished: () => Wahana.Rotation = bearing
            ).Commit(this, "HeadingAnim", length: Application.Current.Resources["AnimLength"] as OnIdiom<byte>, easing: Xamarin.Forms.Easing.SinInOut);
#endif
        }
    }
}