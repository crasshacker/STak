using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;
using STak.TakEngine;

namespace STak.WinTak
{
    public class StoneModel : IPositionable
    {
        private static readonly Random s_spinner = new Random();

        private static int[]  StandingStoneAngles => UIAppConfig.Appearance.StandingStoneAngles;
        private static string StandingStoneSound  => UIAppConfig.Sounds.StandingStone;
        private static string FlatStoneSound      => UIAppConfig.Sounds.FlatStone;
        private static string CapStoneSound       => UIAppConfig.Sounds.CapStone;

        public int               Id           { get; }
        public int               PlayerId     { get; }
        public double            DefaultScale { get; }

        public IMeshModel      MeshModel => m_meshModel;
        public GeometryModel3D Model     => m_meshModel.Model;
        public MeshGeometry3D  Mesh      => m_meshModel.Mesh;
        public Material        Material  => m_meshModel.Material;
        public double          Extent    => m_meshModel.Extent;
        public double          Height    => m_meshModel.Height;

        public bool IsHighlighted => Highlighter?.IsHighlighted == true;

        public double Scale { get => m_meshModel.GetScale().X; private set => m_meshModel.SetScale(value); }

        private readonly IMeshModel         m_meshModel;
        private          StoneType          m_type;
        private          Stone              m_stone;
        private          MeshGeometry3D     m_alternateMesh;
        private          IModelHighlighter  m_highlighter;


        public StoneModel(IMeshModelBuilder meshModelBuilder, int modelId, int playerId, StoneType stoneType,
                                                                          double scale, Scheme scheme = null)
        {
            scheme ??= Scheme.Current;

            Id       = modelId;
            PlayerId = playerId;

            m_type      = stoneType;
            m_meshModel = meshModelBuilder.Build();

            if (stoneType == StoneType.Flat || stoneType == StoneType.Standing)
            {
                m_alternateMesh = BuildStandingStoneMesh();
                if (stoneType == StoneType.Standing)
                {
                    Unflatten();
                }
            }

            Scale        = scale; // Must be done AFTER mesh model has been created.
            DefaultScale = scale;
            ApplyScheme(scheme, playerId);
        }


        public StoneModel(int modelId, int playerId, StoneType stoneType, double scale, Scheme scheme = null)
            : this(GetMeshModelBuilder(stoneType), modelId, playerId, stoneType, scale, scheme)
        {
        }


        private static string GetModelFileName(StoneType stoneType)
            => (stoneType == StoneType.Cap) ? UIAppConfig.Appearance.Models.CapStoneModelFileName
                                            : UIAppConfig.Appearance.Models.FlatStoneModelFileName;

        private static IMeshModelBuilder GetMeshModelBuilder(StoneType stoneType)
            => new JsonMeshModelBuilder(GetModelFileName(stoneType));


        public static string GetSoundFileName(StoneType stoneType)
        {
            return stoneType switch
            {
                StoneType.Cap      => CapStoneSound,
                StoneType.Flat     => FlatStoneSound,
                StoneType.Standing => StandingStoneSound,
                _                  => null
            };
        }


        public IModelHighlighter Highlighter
        {
            get => m_highlighter;

            set
            {
                Highlighter?.Highlight(false);
                m_highlighter = value;
            }
        }


        public Stone Stone
        {
            get
            {
                return m_stone;
            }

            set
            {
                m_stone = value;
                if (m_stone != null)
                {
                    m_stone.Type = m_type;
                }
            }
        }


        public StoneType Type
        {
            get
            {
                return m_type;
            }

            set
            {
                m_type = value;
                if (m_stone != null)
                {
                    m_stone.Type = m_type;
                }
            }
        }


        public double GetOpacity()
        {
            double opacity = 1.0;

            IEnumerable<Material> materials = (Model.Material is MaterialGroup group)
                                            ? (IEnumerable<Material>) group.Children
                                            : (IEnumerable<Material>) new Material[] { Model.Material };

            foreach (Material material in materials)
            {
                if (material is DiffuseMaterial)
                {
                    DiffuseMaterial diffuseMaterial = material as DiffuseMaterial;
                    opacity = diffuseMaterial.Brush.Opacity;
                    break;
                }
            }

            return opacity;
        }


        public void SetOpacity(double opacity)
        {
            IEnumerable<Material> materials = (Model.Material is MaterialGroup group)
                                            ? (IEnumerable<Material>) group.Children
                                            : (IEnumerable<Material>) new Material[] { Model.Material };

            foreach (Material material in materials)
            {
                if (material is DiffuseMaterial)
                {
                    DiffuseMaterial diffuseMaterial = material as DiffuseMaterial;
                    Brush brush = (Brush) diffuseMaterial.Brush.Clone();
                    brush.Opacity = opacity;
                    diffuseMaterial.Brush = brush;
                    diffuseMaterial.Brush.Freeze();
                }
            }
        }


        public void Highlight(bool highlight)
        {
            if (IsHighlighted != highlight)
            {
                Highlighter?.Highlight(highlight);
            }
        }


        public void ApplyScheme(Scheme scheme, int playerId)
        {
            string textureFile = (playerId == Player.One) ? scheme.P1StoneTextureFile : scheme.P2StoneTextureFile;

            EmissiveMaterial emissiveMaterial = MaterialHelper.GetEmissiveMaterial(textureFile);
            DiffuseMaterial  diffuseMaterial  = MaterialHelper.GetDiffuseMaterial(textureFile);

            MaterialGroup stoneMaterial = new MaterialGroup();
            stoneMaterial.Children.Add(emissiveMaterial);
            stoneMaterial.Children.Add(diffuseMaterial);
            Model.Material = stoneMaterial;

            // Important! Reset highlighting prior to discarding the highlighter.  Failing to do so will
            // leave the animation listening for CompositionTarget.Rendering events (and receiving them),
            // so it will continue its animation until a new game is started and this stone is replaced.
            Highlighter = GetConfiguredHighlighter();
        }


        public void SetPosition(Point3D point)
        {
            m_meshModel.SetPosition(point);

            if (point.X > -475 && point.X <= 475
             && point.Z > -475 && point.Z <= 475
                               && point.Y <  100)
            {
                // The stone is either below the board or is partially contained within it.  This should
                // never happen - but it sometimes does on hub-based games.  Needs diagnosis.
                // Debugger.Break();
            }
        }


        public Point3D GetPosition()
        {
            return m_meshModel.GetPosition();
        }


        public Rect Get2DBounds()
        {
            Point3D center = Model.Transform.Transform(new Point3D());
            double e = Extent;
            double x = center.X - e/2;
            double y = center.Z - e/2;
            return new Rect(x, y, e, e);
        }


        public void Flatten()
        {
            if (Type == StoneType.Standing)
            {
                if (Stone != null)
                {
                    Stone.Type = StoneType.Flat;
                }

                Type = StoneType.Flat;
                SwapMeshes();
            }
        }


        public void Unflatten()
        {
            if (Type == StoneType.Flat)
            {
                if (Stone != null)
                {
                    Stone.Type = StoneType.Standing;
                }

                Type = StoneType.Standing;
                SwapMeshes();
            }
        }


        public void AlignToStone()
        {
            if (Type == StoneType.Flat && Stone.Type == StoneType.Standing)
            {
                Unflatten();
            }
            else if (Type == StoneType.Standing && Stone.Type == StoneType.Flat)
            {
                Flatten();
            }
        }


        private IModelHighlighter GetConfiguredHighlighter()
        {
            IModelHighlighter modelHighlighter = null;

            if (UIAppConfig.Appearance.Animation.LegalMoveHighlighting != null)
            {
                ForwardingModelHighlighter highlighter = new ForwardingModelHighlighter();
                modelHighlighter = highlighter;

                foreach (var highlightSettings in UIAppConfig.Appearance.Animation.LegalMoveHighlighting)
                {
                    if (String.Equals(highlightSettings.PulseType, "opacity", StringComparison.OrdinalIgnoreCase))
                    {
                        int    pulseCount     = highlightSettings.PulseCount;
                        int    pulseDuration  = highlightSettings.PulseDuration;
                        double stoppingRate   = highlightSettings.StoppingRate;
                        double minimumOpacity = highlightSettings.MinimumValue;
                        double maximumOpacity = highlightSettings.MaximumValue;
                        double defaultOpacity = highlightSettings.DefaultValue;

                        var duration = TimeSpan.FromMilliseconds(pulseDuration);

                        highlighter.Add(new PulsatingStoneModelHighlighter(Id, pulseCount, duration, minimumOpacity,
                                                     maximumOpacity, defaultOpacity, stoppingRate, () => GetOpacity(),
                                                                                                  (v) => SetOpacity(v)));
                    }
                    else if (String.Equals(highlightSettings.PulseType, "scale", StringComparison.OrdinalIgnoreCase))
                    {
                        int    pulseCount    = highlightSettings.PulseCount;
                        int    pulseDuration = highlightSettings.PulseDuration;
                        double stoppingRate  = highlightSettings.StoppingRate;
                        double minimumScale  = highlightSettings.MinimumValue;
                        double maximumScale  = highlightSettings.MaximumValue;
                        double defaultScale  = highlightSettings.DefaultValue;

                        minimumScale *= DefaultScale;
                        maximumScale *= DefaultScale;
                        defaultScale *= DefaultScale;

                        var duration = TimeSpan.FromMilliseconds(pulseDuration);

                        highlighter.Add(new PulsatingStoneModelHighlighter(Id, pulseCount, duration, minimumScale,
                                         maximumScale, defaultScale, stoppingRate, () => m_meshModel.GetScale().X,
                                                                                  (v) => m_meshModel.SetScale(v)));
                    }
                }
            }

            return modelHighlighter;
        }


        private void SwapMeshes()
        {
            var mesh = m_meshModel.Mesh;
            m_meshModel.Mesh = m_alternateMesh;
            m_alternateMesh = mesh;
        }


        private MeshGeometry3D BuildStandingStoneMesh()
        {
            Transform3D GetStandingStoneTransform()
            {
                // First we'll raise the stone to the necessary height in preparation for rotating.
                Transform3D translateTransform1 = new TranslateTransform3D(0, Extent/2, 0);

                // Next we'll rotate it about its center into a standing position.
                AxisAngleRotation3D axisAngle1  = new AxisAngleRotation3D(new Vector3D(0, 0, 1), 90);
                Transform3D rotateTransform1    = new RotateTransform3D(axisAngle1, 0, Extent/2, 0);

                // Finally we'll rotate it so that it's out of alignment with the cell (not strictly necessary).
                int angle = StandingStoneAngles[s_spinner.Next(StandingStoneAngles.Length)];
                AxisAngleRotation3D axisAngle2  = new AxisAngleRotation3D(new Vector3D(0, 1, 0),
                                                     (angle < 0) ? s_spinner.Next(360) : angle);
                Transform3D rotateTransform2    = new RotateTransform3D(axisAngle2);

                Transform3DGroup transformGroup = new Transform3DGroup();
                transformGroup.Children.Add(translateTransform1);
                transformGroup.Children.Add(rotateTransform1);
                transformGroup.Children.Add(rotateTransform2);

                return new MatrixTransform3D(transformGroup.Value);
            }

            var mesh = m_meshModel.Mesh.Clone();
            var transform = GetStandingStoneTransform();
            Point3D[] points = mesh.Positions.ToArray();
            transform.Value.Transform(points);
            mesh.Positions = new Point3DCollection(points);
            return mesh;
        }
    }
}
