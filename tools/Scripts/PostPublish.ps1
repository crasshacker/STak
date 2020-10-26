#
# NOTE - This script ASSUMES it's located in src/Tools/Scripts ($PSScriptRoot).
#        Also, this is a complete HACK, but VS and dotnet publish don't provide
#        any means of executing tasks after the publish step completes.
#

param
(
    [Parameter(Mandatory=$false)]
    [string[]] $Project = @("WinTak", "TakHub"),

    [Parameter(Mandatory=$false)]
    [string[]] $Config = @("Debug", "Release"),

    [Parameter(Mandatory=$false)]
    [string[]] $TargetFramework = "net5.0"
)

$wintak_target_framework = "$TargetFramework-windows"
$takhub_target_framework = "$TargetFramework"

$script_dir     = $PSScriptRoot
$tak_dir        = (Resolve-Path "$script_dir/../..").Path
$src_dir        = "$tak_dir/src"
$app_dir        = "$src_dir/app"

foreach ($release_type in $Config)
{
    if ($Project -contains "WinTak")
    {
        $wintak_src_dir = "$app_dir/WinTak"

        $wintak_bin_dir = "$wintak_src_dir/bin/$release_type/$wintak_target_framework"
        $wintak_doc_dir = "$wintak_bin_dir/Resources/Documents"
        $models_bin_dir = "$wintak_bin_dir/Resources/Models"
        $font_dir       = "$wintak_bin_dir/Resources/Fonts"
        $plugin_bin_dir = "$wintak_bin_dir/Plugins"

        $wintak_pub_dir     = "$wintak_bin_dir/publish"
        $plugin_pub_dir     = "$wintak_pub_dir/Plugins"
        $models_pub_dir     = "$wintak_pub_dir/Resources/Models"
        $font_pub_dir       = "$wintak_pub_dir/Resources/Fonts"
        $wintak_doc_pub_dir = "$wintak_pub_dir/Resources/Documents"

        mkdir -Force $wintak_pub_dir     -ErrorAction SilentlyContinue | Out-Null
        mkdir -Force $models_pub_dir     -ErrorAction SilentlyContinue | Out-Null
        mkdir -Force $plugin_pub_dir     -ErrorAction SilentlyContinue | Out-Null
        mkdir -Force $font_pub_dir       -ErrorAction SilentlyContinue | Out-Null
        mkdir -Force $wintak_doc_pub_dir -ErrorAction SilentlyContinue | Out-Null

        cp -Force -Verbose "$wintak_bin_dir/uiappsettings.json"      $wintak_pub_dir
        cp -Force -Verbose "$wintak_bin_dir/interopappsettings.json" $wintak_pub_dir

        cp -Force -Verbose "$models_bin_dir/BoardModel.json"         $models_pub_dir
        cp -Force -Verbose "$models_bin_dir/CapStoneModel.json"      $models_pub_dir
        cp -Force -Verbose "$models_bin_dir/FlatStoneModel.json"     $models_pub_dir
        cp -Force -Verbose "$models_bin_dir/GridLineModel.json"      $models_pub_dir

        cp -Force -Verbose "$wintak_doc_dir/github-markdown.css"     $wintak_doc_pub_dir
        cp -Force -Verbose "$wintak_doc_dir/ProjectStatus.html"      $wintak_doc_pub_dir
        cp -Force -Verbose "$wintak_doc_dir/UserGuide.html"          $wintak_doc_pub_dir

        cp -Force -Verbose "$wintak_doc_dir/github-markdown.css"     $wintak_doc_pub_dir
        cp -Force -Verbose "$wintak_doc_dir/ProjectStatus.html"      $wintak_doc_pub_dir
        cp -Force -Verbose "$wintak_doc_dir/UserGuide.html"          $wintak_doc_pub_dir

        cp -Force -Verbose "$font_dir/Army of Darkness.ttf"          $font_pub_dir
        cp -Force -Verbose "$font_dir/Atama__G.ttf"                  $font_pub_dir
        cp -Force -Verbose "$font_dir/Baldur Regular.ttf"            $font_pub_dir
        cp -Force -Verbose "$font_dir/Celtic Bold.ttf"               $font_pub_dir
        cp -Force -Verbose "$font_dir/Celtic Normal.ttf"             $font_pub_dir
        cp -Force -Verbose "$font_dir/Dirty Headline.ttf"            $font_pub_dir
        cp -Force -Verbose "$font_dir/FZ JAZZY 14 3D.ttf"            $font_pub_dir
        cp -Force -Verbose "$font_dir/RAVIE.TTF"                     $font_pub_dir
        cp -Force -Verbose "$font_dir/Showcard Gothic.ttf"           $font_pub_dir

        cp -Force -Verbose "$plugin_bin_dir/*.dll"                   $plugin_pub_dir
        cp -Force -Verbose "$plugin_bin_dir/*.cs"                    $plugin_pub_dir
    }

    if ($Project -contains "TakHub")
    {
        $takhub_src_dir = "$app_dir/TakHub/Server"
        $takhub_doc_dir = "$takhub_bin_dir"
        $takhub_bin_dir = "$takhub_src_dir/bin/$release_type/$takhub_target_framework"
        $plugin_bin_dir = "$takhub_bin_dir/Plugins"

        $takhub_pub_dir     = "$takhub_bin_dir/publish"
        $plugin_pub_dir     = "$takhub_pub_dir/Plugins"
        $takhub_doc_pub_dir = "$takhub_pub_dir"

        mkdir -Force $plugin_pub_dir -ErrorAction SilentlyContinue | Out-Null

        cp -Force -Verbose "$takhub_doc_dir/github-markdown.css"     $takhub_doc_pub_dir
        cp -Force -Verbose "$takhub_doc_dir/ProjectStatus.html"      $takhub_doc_pub_dir
        cp -Force -Verbose "$takhub_doc_dir/UserGuide.html"          $takhub_doc_pub_dir

        cp -Force -Verbose "$plugin_bin_dir/*.dll"                   $plugin_pub_dir
        cp -Force -Verbose "$plugin_bin_dir/*.cs"                    $plugin_pub_dir
    }
}
