param
(
    [Parameter(Mandatory=$false)]
    [switch] $Classic
)

$src_dir    = Convert-Path "$PSScriptRoot/../../src"
$app_dir    = "$src_dir/app"
$wintak_dir = "$app_dir/WinTak"
$takhub_dir = "$app_dir/TakHub/Server"

if ($Classic)
{
    $assembly_info = "$wintak_dir/Properties/AssemblyInfo.cs"
    $version_info1 = Get-Content $assembly_info | sls '^\[assembly: *AssemblyFileVersion\("\d+\.\d+\.\d+\.\d*"\)]'

    if ($version_info1 -notmatch '(\d+)\.(\d+)\.(\d+)\.(\d+)')
    {
        throw "Failed to extract version information from $assembly_info."
    }

    $assembly_major = $matches[1]
    $assembly_minor = $matches[2]
    $assembly_build = $matches[3]
    $assembly_revno = $matches[4]

    Write-Output "$assembly_major.$assembly_minor.$assembly_build.$assembly_revno"
}
else
{
    $about_dialog  = "$wintak_dir/Dialogs/AboutDialog.xaml"
    $version_info2 = Get-Content $about_dialog | sls '>\s*Version \d+\.\d+\.\d+(\.\d+)?\s*<'

    if ($version_info2 -notmatch '(\d+)\.(\d+)\.(\d+)(\.(\d+))?')
    {
        throw "Failed to extract version information from $about_dialog."
    }

    $semantic_major = $matches[1]
    $semantic_minor = $matches[2]
    $semantic_build = $matches[3]

    Write-Output "$semantic_major.$semantic_minor.$semantic_build"
}
