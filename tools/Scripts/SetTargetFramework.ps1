param
(
    [Parameter(Mandatory=$false)]
    [string[]] $TargetFramework = "net6.0"
)

$InformationPreference = 'Continue'

$script_dir = Convert-Path $PSScriptRoot
$tak_dir    = "$script_dir/../.."
$src_dir    = "$tak_dir/src"

pushd $src_dir

$project_files = git ls-files | Select-String '\.csproj$'

$xml_tag = "TargetFramework" + (($TargetFramework.Length -gt 1) ? "s" : "")

$windows_projects = @('WinTak')

$generic_frameworks = ($TargetFramework -join ",")
$windows_frameworks = ($TargetFramework | %{ "$_-windows" }) -join ","

function AreListsEqual
{
    foreach ($x in $args[0]) { if (! $args[1] -contains $x) { return $false } }
    foreach ($y in $args[1]) { if (! $args[0] -contains $y) { return $false } }
    return true
}

foreach ($file in $project_files)
{
    $updated = $false
    $lines = @()

    foreach ($line in (Get-Content $file))
    {
        if ($line -match '^(.*)<TargetFrameworks?>(.*)<\/TargetFrameworks?>(.*)')
        {
            $old_frameworks = $2 -split ','
            $new_frameworks = $generic_frameworks -split ','

            if (-not (AreListsEqual $old_frameworks, $new_frameworks))
            {
                $frameworks = $windows_projects -contains ($file -replace ".*?([^/]*)\.csproj$", '$1') `
                                                           ? $windows_frameworks : $generic_frameworks
                $line = $line -replace '^(.*)<TargetFrameworks?>.*<\/TargetFrameworks?>(.*)',
                                                      "`$1<$xml_tag>$frameworks</$xml_tag>`$2"
                $updated = $true
            }
        }
        $lines += $line
    }

    if ($updated)
    {
        $lines | Set-Content $file -Encoding UTF8BOM
        Write-Verbose "Updated project file $file"
    }
    else
    {
        Write-Verbose "Did not update project file $file"
    }
}

popd
