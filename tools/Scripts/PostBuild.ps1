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

$ErrorActionPreference = "Stop"
$InformationPreference = "Continue"
$VerbosePreference     = "Continue"

# Remove "-windows" extension if it was specified.
$TargetFramework = $TargetFramework -replace "-windows$", "" 

$wintak_target_framework = "$TargetFramework-windows"
$takhub_target_framework = "$TargetFramework"

$script_dir     = $PSScriptRoot
$markitdown_dir = (Resolve-Path "$script_dir\..\MarkItDown").Path
$tak_dir        = (Resolve-Path "$script_dir\..\..").Path
$src_dir        = "$tak_dir\src"
$app_dir        = "$src_dir\app"
$doc_src_dir    = "$tak_dir\Docs"
$plugin_dir     = "$src_dir\lib\Engine\ExperimentalTakAI"

function ProcessMarkdownDocs
{
    $applystyles = "$script_dir\ApplyGitHubStyles.ps1" 
    $markitdown  = "$markitdown_dir\MarkItDown.exe"

    $css_file = "$doc_src_dir\github-markdown.css"
    $md_files = "$doc_src_dir\UserGuide.md",
                "$doc_src_dir\ProjectStatus.md"

    if ($Project -contains "WinTak")
    {
        mkdir -Force -Verbose $wintak_doc_dir -ErrorAction Continue | Out-Null

        copy -Verbose -Force $css_file $wintak_doc_dir

        foreach ($file in $md_files)
        {
            $basename = [IO.Path]::GetFileNameWithoutExtension((Resolve-Path $file).Path)
            Write-Verbose "MarkItDown: $file => $wintak_doc_dir\$basename-TEMP.html"
            & $markitdown $file > "$wintak_doc_dir\$basename-TEMP.html"
            Write-Verbose "Styling $wintak_doc_dir\$basename-TEMP.html => $wintak_doc_dir\$basename.html"
            & $applystyles "$wintak_doc_dir\$basename-TEMP.html" "$wintak_doc_dir\$basename.html"
            rm -Force -Verbose "$wintak_doc_dir\$basename-TEMP.html"
        }
    }

    if ($Project -contains "TakHub")
    {
        mkdir -Force -Verbose $takhub_doc_dir -ErrorAction Continue | Out-Null

        $applystyles = "$script_dir\ApplyGitHubStyles.ps1" 
        $markitdown  = "$markitdown_dir\MarkItDown.exe"

        $css_file = "$doc_src_dir\github-markdown.css"
        $md_files = "$doc_src_dir\UserGuide.md",
                    "$doc_src_dir\ProjectStatus.md"

        copy -Verbose -Force $css_file $takhub_doc_dir

        foreach ($file in $md_files)
        {
            $basename = [IO.Path]::GetFileNameWithoutExtension((Resolve-Path $file).Path)
            Write-Verbose "MarkItDown: $file => $takhub_doc_dir\$basename-TEMP.html"
            & $markitdown $file > "$takhub_doc_dir\$basename-TEMP.html"
            Write-Verbose "Styling $takhub_doc_dir\$basename-TEMP.html => $takhub_doc_dir\$basename.html"
            & $applystyles "$takhub_doc_dir\$basename-TEMP.html" "$takhub_doc_dir\$basename.html"
            rm -Force -Verbose "$takhub_doc_dir\$basename-TEMP.html"
        }
    }
}

function ProcessPlugins
{
    if ($Project -contains "WinTak")
    {
        if (! (Test-Path "$wintak_plugin_bin_dir"))
        {
            mkdir "$wintak_plugin_bin_dir" | Out-Null
        }

        dir -Recurse -Verbose -File -Filter "*TakAI.cs" $plugin_dir | `
                                    select -ExpandProperty FullName | `
                         %{ copy -Verbose $_ $wintak_plugin_bin_dir }

        # Note: Exclude ExperimentalTakAI; we include source instead.
        dir -Recurse -Verbose -File -Filter "*TakAI.dll" $plugin_dir | `
                              where Name -ne "ExperimentalTakAI.dll" | `
                                     select -ExpandProperty FullName | `
                          %{ copy -Verbose $_ $wintak_plugin_bin_dir }
    }
    if ($Project -contains "TakHub")
    {
        if (! (Test-Path "$takhub_plugin_bin_dir"))
        {
            mkdir "$takhub_plugin_bin_dir" | Out-Null
        }

        dir -Recurse -Verbose -File -Filter "*TakAI.cs" $plugin_dir | `
                                    select -ExpandProperty FullName | `
                         %{ copy -Verbose $_ $takhub_plugin_bin_dir }

        # Note: Exclude ExperimentalTakAI; we include source instead.
        dir -Recurse -Verbose -File -Filter "*TakAI.dll" $plugin_dir | `
                              where Name -ne "ExperimentalTakAI.dll" | `
                                     select -ExpandProperty FullName | `
                          %{ copy -Verbose $_ $takhub_plugin_bin_dir }
    }
}

foreach ($release_type in $Config)
{
    $wintak_src_dir = "$app_dir\WinTak"
    $wintak_bin_dir = "$wintak_src_dir\bin\$release_type\$wintak_target_framework"
    $wintak_doc_dir = "$wintak_bin_dir\Resources\Documents"

    $takhub_src_dir = "$app_dir\TakHub\Server"
    $takhub_bin_dir = "$takhub_src_dir\bin\$release_type\$takhub_target_framework"
    $takhub_doc_dir = "$takhub_bin_dir"

    $wintak_plugin_bin_dir = "$wintak_bin_dir\Plugins"
    $takhub_plugin_bin_dir = "$takhub_bin_dir\Plugins"

    ProcessMarkdownDocs
    ProcessPlugins
}
