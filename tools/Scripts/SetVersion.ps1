#
# SetVersion.ps1 - Set the version number in AssemblyInfo.cs and AboutDialog.xaml.
#
# Usage: SetVersion.ps1 <version>
#
# Here <version> must have the format: major.minor.build[.revision]
#
param
(
    [Parameter(Mandatory=$true)]
    [string] $Version,

    [Parameter(Mandatory=$false)]
    [switch] $ClassicVersioning
)

$wintak_dir    = Convert-Path "$PSScriptRoot/../../src/app/WinTak"
$takhub_dir    = Convert-Path "$PSScriptRoot/../../src/app/TakHub/Server"
$assembly_info = Convert-Path "$wintak_dir/Properties/AssemblyInfo.cs"
$about_dialog  = Convert-Path "$wintak_dir/Dialogs/AboutDialog.xaml"
$project_file  = Convert-Path "$takhub_dir/TakHub.csproj"

if ($Version -notmatch '(\d+)\.(\d+)\.(\d+)\.?(\d+)?')
{
    throw "Invalid version; value must have the form: major.minor.build[.revision]";
}

$major = $matches[1]
$minor = $matches[2]
$build = $matches[3]
$revno = if ($matches.Count -gt 4) { $matches[4] } else { 0 }

$semantic_version = "$major.$minor.$build"
$classic_version  = "$major.$minor.$build.$revno"

$ui_version = $ClassicVersioning ? $classic_version : $semantic_version

# AssemblyInfo.cs
(Get-Content $assembly_info | foreach `
{
    $_ -replace '^\[assembly: *AssemblyVersion\("\d+\.\d+\.\d+\.\d*"\)]', `
                  "[assembly: AssemblyVersion(`"$classic_version`")]" `
       -replace '^\[assembly: *AssemblyFileVersion\("\d+\.\d+\.\d+\.\d*"\)]', `
                   "[assembly: AssemblyFileVersion(`"$classic_version`")]"
}) | Out-File -Force $assembly_info

# AboutDialog.xaml
(Get-Content $about_dialog | foreach `
{
    $_ -replace 'Version \d+\.\d+\.\d+(\.\d+)?', "Version $ui_version"
}) | Out-File -Force $about_dialog

# TakHub.csproj
(Get-Content $project_file | foreach `
{
    $_ -replace '^(?<lead>\s*)<Version>\d+\.\d+\.\d+(\.\d+)?</Version>', `
                 "`${lead}<Version>${ui_version}</Version>"
}) | Out-File -Force $project_file
