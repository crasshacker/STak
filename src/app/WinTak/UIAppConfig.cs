using System;
using System.Drawing;           // For the Size struct with integer properties (unlike System.Windows.Size).
using STak.TakEngine;
using STak.TakEngine.AI;
using Microsoft.Extensions.Configuration;

namespace STak.WinTak
{
    public static class UIAppConfig
    {
        private static readonly IConfigurationRoot s_config;
        private static          UISettings         s_appSettings;

        public static bool                            AutoReload   => s_appSettings.AutoReload;
        public static UISettings.FrameworkSettings    Framework    => s_appSettings.Framework;
        public static UISettings.BehaviorSettings     Behavior     => s_appSettings.Behavior;
        public static UISettings.AppearanceSettings   Appearance   => s_appSettings.Appearance;
        public static UISettings.MoveTrackingSettings MoveTracking => s_appSettings.MoveTracking;
        public static UISettings.StickyWindowSettings StickyWindow => s_appSettings.StickyWindow;
        public static UISettings.SoundsSettings       Sounds       => s_appSettings.Sounds;


        static UIAppConfig()
        {
            s_config = new ConfigurationBuilder()
                .AddJsonFile("uiappsettings.json", true, true)
                .AddEnvironmentVariables()
                .Build();

            s_appSettings = s_config.Get<UISettings>();
        }


        public static void Refresh()
        {
            s_appSettings = s_config.Get<UISettings>();
        }
    }


    public class UISettings
    {
        public bool                 AutoReload   { get; set; }
        public FrameworkSettings    Framework    { get; set; }
        public BehaviorSettings     Behavior     { get; set; }
        public TakAIOptions         AIBehavior   { get; set; }
        public AppearanceSettings   Appearance   { get; set; }
        public MoveTrackingSettings MoveTracking { get; set; }
        public StickyWindowSettings StickyWindow { get; set; }
        public SoundsSettings       Sounds       { get; set; }


        public class FrameworkSettings
        {
            public bool   UseMirroringGame   { get; set; }
            public bool   UseActorSystem     { get; set; }
            public string ActorSystemAddress { get; set; }
        }


        public class BehaviorSettings
        {
            public bool   AllowUnsafeOperations    { get; set; }
            public bool   UseVerbosePtnNotation    { get; set; }
            public string DetachGameUponCompletion { get; set; }
        }


        public class MoveTrackingSettings
        {
            public int    NotificationInterval  { get; set; }
            public double StoneSnappingZone     { get; set; }
            public double StoneHighlightingZone { get; set; }
        }


        public class StickyWindowSettings
        {
            public int  EventHorizon  { get; set; }
            public bool StickToScreen { get; set; }
            public bool StickToWindow { get; set; }
            public bool StickOnResize { get; set; }
            public bool StickOnMove   { get; set; }
        }


        public class AppearanceSettings
        {
            public BannerSettings    Banner           { get; set; }
            public AnimationSettings Animation        { get; set; }
            public ModelsSettings    Models           { get; set; }
            public LightsSettings    Lights           { get; set; }

            public string AudioPlayer                 { get; set; }
            public double BoardRotationDelta          { get; set; }
            public double BoardZoomDistance           { get; set; }
            public int[]  StandingStoneAngles         { get; set; }
            public bool   HighlightMovingPlayerStones { get; set; }
            public bool   HighlightMovingAIStones     { get; set; }
            public bool   HighlightWinningStones      { get; set; }
            public bool   ClipTableViewToBounds       { get; set; }
            public bool   AllowInfiniteZoom           { get; set; }
            public string GameRulesTextUrl            { get; set; }
            public string GameRulesVideoUrl           { get; set; }
        }


        public class BannerSettings
        {
            public string[] FontNames     { get; set; }
            public int[]    FontSizes     { get; set; }
            public int      AnimationTime { get; set; }
            public string[] GameWinQuips  { get; set; }
            public string[] GameLossQuips { get; set; }
            public string[] GameDrawQuips { get; set; }
            public string[] GameOverQuips { get; set; }
        }


        public class AnimationSettings
        {
            public HighlightSettings[] LegalMoveHighlighting { get; set; }
            public HighlightSettings[] GameWinHighlighting   { get; set; }

            public bool   HighlightWhenInGrabZone   { get; set; }
            public bool   HighlightWhenInDropZone   { get; set; }
            public bool   AnimateMoves              { get; set; }
            public bool   AnimateBoard              { get; set; }
            public double AnimationAirGap           { get; set; }
            public bool   RotateBoardToFacePlayer   { get; set; }
            public int    BoardZoomAnimationTime    { get; set; }
            public int    BoardResetAnimationTime   { get; set; }
            public int    MoveAnimationTime         { get; set; }
            public int    HintAnimationPauseTime    { get; set; }
            public double AnimationSpeedFactor      { get; set; }
        }


        public class HighlightSettings
        {
            public string PulseType     { get; set; }
            public int    PulseCount    { get; set; }
            public int    PulseDuration { get; set; }
            public double MinimumValue  { get; set; }
            public double MaximumValue  { get; set; }
            public double DefaultValue  { get; set; }
            public double StoppingRate  { get; set; }
        }


        public class SoundsSettings
        {
            public string[] WinGame       { get; set; }
            public string[] LoseGame      { get; set; }
            public string   StandingStone { get; set; }
            public string   FlatStone     { get; set; }
            public string   CapStone      { get; set; }
        }


        public class ModelsSettings
        {
            public string BoardModelFileName     { get; set; }
            public string GridLineModelFileName  { get; set; }
            public string FlatStoneModelFileName { get; set; }
            public string CapStoneModelFileName  { get; set; }
            public double StoneToCellWidthRatio  { get; set; }
        }


        public class LightsSettings
        {
            public DirectionalLightData[] DirectionalLights { get; set; }
        }


        public class DirectionalLightData
        {
            public double[] Direction { get; set; }
            public string   Color     { get; set; }
        }
    }
}
