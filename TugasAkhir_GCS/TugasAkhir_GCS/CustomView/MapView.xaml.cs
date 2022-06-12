using BruTile.Cache;
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

namespace TugasAkhir_GCS
{
    public partial class MapView : ContentView
    {
        Pin Wahana;

        public MapView()
        {
            InitializeComponent();

            mapView.MyLocationLayer.Enabled = false;
            mapView.RotationLock = false;

            mapView.Map.CRS = "EPSG:3857";
            mapView.Map.Transformation = new MinimalTransformation();
            mapView.Map.Layers.Add(new TileLayer(
                KnownTileSources.Create(
                    KnownTileSource.BingAerial,
                    Variables.BING_MAPS_API_KEY,
                    new BruTile.Cache.FileCache(
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
                TextAlignment = Mapsui.Widgets.Alignment.Center,
                HorizontalAlignment = Mapsui.Widgets.HorizontalAlignment.Right,
                VerticalAlignment = Mapsui.Widgets.VerticalAlignment.Bottom,
                MarginY = 30,
            });

            mapView.Map.Home = x => x.NavigateTo(new Mapsui.Geometries.BoundingBox(
                SphericalMercator.FromLonLat(lat: -11.00750, lon: 94.97278),
                SphericalMercator.FromLonLat(lat: 6.07500, lon: 141.01944)
            ), ScaleMethod.Fit);
        }

        public void UpdateGPS(int lat, int lon)
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

                Wahana.Position = new Position(lat / 10000000.0, lon / 10000000.0);

                mapView.Navigator.NavigateTo(
                    SphericalMercator.FromLonLat(Wahana.Position.Longitude, Wahana.Position.Latitude),
                    ZoomLevelExtensions.ToMapsuiResolution(18.5), 3000, Mapsui.Utilities.Easing.CubicInOut);

                return;
            }

            //Wahana.Position = new Position(lat / 10000000.0, lon / 10000000.0);

            var asal_pos = Wahana.Position;
            var temp_pos = new Position(lat / 10000000.0, lon / 10000000.0);

            AnimationExtensions.Animate<Position>(self: this, name: "PositionAnim", rate: 5, length: 30,
            transform: (time) =>
            {
                var lat_interpolated = asal_pos.Latitude
                                    + ((temp_pos.Latitude - asal_pos.Latitude)
                                    * time);

                var lon_interpolated = asal_pos.Longitude
                                    + ((temp_pos.Longitude - asal_pos.Longitude)
                                    * time);

                return new Position(lat_interpolated, lon_interpolated);
            },
            callback: (val) => Wahana.Position = val,
            finished: (endpos, finished) => Wahana.Position = endpos);
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

            //Wahana.Rotation = bearing;

            new Xamarin.Forms.Animation(start: Wahana.Rotation, end: hdg,
            callback: val => Wahana.Rotation = (float)val,
            finished: () => Wahana.Rotation = bearing
            ).Commit(this, "HeadingAnim", rate: 10, length: 30, easing: Xamarin.Forms.Easing.SinInOut);
        }
    }
}