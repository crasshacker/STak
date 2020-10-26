param
(
    [Parameter(Mandatory=$false)]
    [string[]] $Project = @("WinTak", "TakHub"),

    [Parameter(Mandatory=$false)]
    [string[]] $Config = @("Debug", "Release"),

    [Parameter(Mandatory=$false)]
    [string[]] $Architecture = @("x64"),

    [Parameter(Mandatory=$false)]
    [string[]] $TargetFramework = "net5.0"
)

$ErrorActionPreference = "Stop"
$VerbosePreference     = "Continue"
$WarningPreference     = "Continue"
$DebugPreference       = "Continue"

$wintak_target_framework = "$TargetFramework-windows"
$takhub_target_framework = "$TargetFramework"

$script_dir     = Convert-Path $PSScriptRoot
$tak_dir        = "$script_dir/../.."
$src_dir        = "$tak_dir/src"
$app_dir        = "$src_dir/app"
$wintak_src_dir = "$app_dir/WinTak"
$takhub_src_dir = "$app_dir/TakHub/Server"

$version     = & "$script_dir/GetVersion.ps1"
$tak_src_zip = "Tak-src-${version}.zip" 
$wintak_rids = ,"" + ($Architecture | %{ "win-$_" })
$takhub_rids = ,"" + ($Architecture | %{ "win-$_", "linux-$_", "osx-$_" })
$standalone  = ,"true"

pushd $tak_dir

#
# Build a zip of the project source code, docs, etc.
#
Write-Verbose "***** Building source code zip file."
7z a -mx9 -bb2 $tak_src_zip (git ls-files)
mv -Force -Verbose $tak_src_zip $tak_dir

foreach ($current_config in $Config)
{
    $wintak_rid_standalone_bin_zip = "WinTak-RID-standalone-bin-${current_config}-${version}.zip" 
    $takhub_rid_standalone_bin_zip = "TakHub-RID-standalone-bin-${current_config}-${version}.zip" 

    $wintak_rid_dependent_bin_zip = "WinTak-RID-dependent-bin-${current_config}-${version}.zip" 
    $takhub_rid_dependent_bin_zip = "TakHub-RID-dependent-bin-${current_config}-${version}.zip" 

    $wintak_portable_zip = "WinTak-bin-${current_config}-${version}.zip" 
    $takhub_portable_zip = "TakHub-bin-${current_config}-${version}.zip" 

    $wintak_bin_dir = "$wintak_src_dir/bin/$current_config/$wintak_target_framework"
    $takhub_bin_dir = "$takhub_src_dir/bin/$current_config/$takhub_target_framework"

    if ($Project -contains "WinTak")
    {
        ##
        ## Build and publish WinTak.
        ##

        foreach ($wintak_rid in $wintak_rids)
        {
            foreach ($self_contained in $standalone)
            {
                cd $wintak_src_dir

                Write-Verbose "***** Cleaning up old builds (removing bin and obj directories."

                #
                # Clean up prior to build.
                #
                rm -Recurse "$wintak_src_dir/obj" -Force -Verbose -ErrorAction SilentlyContinue | Out-Null
                rm -Recurse "$wintak_src_dir/bin" -Force -Verbose -ErrorAction SilentlyContinue | Out-Null

                Write-Verbose "***** Building and publishing $current_config $wintak_rid WinTak."

                if ($wintak_rid)
                {
                    dotnet clean   -c $current_config -r $wintak_rid -f $wintak_target_framework
                    dotnet publish -c $current_config -r $wintak_rid -f $wintak_target_framework --self-contained $self_contained
                    mv -Force "$wintak_bin_dir/$wintak_rid/publish" $wintak_bin_dir
                }
                else
                {
                    dotnet clean   -c $current_config -f $wintak_target_framework
                    dotnet publish -c $current_config -f $wintak_target_framework
                }

                Write-Verbose "***** Post-publishing $current_config $wintak_rid WinTak."

                cd $wintak_bin_dir

                & "$script_dir/PostPublish.ps1" -Project WinTak -Config $current_config
                rm -Force -Verbose -Recurse WinTak -ErrorAction SilentlyContinue | Out-Null
                mv -Force -Verbose publish WinTak

                Write-Verbose "***** Building WinTak $current_config $wintak_rid zip file."

                $zip_file = $wintak_portable_zip;

                if ($wintak_rid)
                {
                    $zip_file = if ($self_contained -eq "true") { $wintak_rid_standalone_bin_zip } `
                                                           else { $wintak_rid_dependent_bin_zip }
                    $zip_file = $zip_file -replace "RID", $wintak_rid
                }

                7z a -r -mx9 -bb2 $zip_file WinTak
                mv -Force -Verbose $zip_file $tak_dir
            }
        }
    }

    if ($Project -contains "TakHub")
    {
        ##
        ## Build and publish TakHub.
        ##

        foreach ($takhub_rid in $takhub_rids)
        {
            foreach ($self_contained in $standalone)
            {
                cd $takhub_src_dir

                #
                # Clean up prior to build.
                #
                rm -Recurse "$takhub_src_dir/obj" -Force -Verbose -ErrorAction SilentlyContinue | Out-Null
                rm -Recurse "$takhub_src_dir/bin" -Force -Verbose -ErrorAction SilentlyContinue | Out-Null

                #
                # Build and publish standalone OS-specific TakHub.
                #
                Write-Verbose "***** Building and publishing $current_config $takhub_rid TakHub."

                if ($takhub_rid)
                {
                    dotnet clean   -c $current_config -r $takhub_rid -f $takhub_target_framework
                    dotnet publish -c $current_config -r $takhub_rid -f $takhub_target_framework --self-contained $self_contained
                    mv -Force "$takhub_bin_dir/$takhub_rid/publish" $takhub_bin_dir
                }
                else
                {
                    dotnet clean   -c $current_config -f $takhub_target_framework
                    dotnet publish -c $current_config -f $takhub_target_framework
                }

                cd $takhub_bin_dir

                Write-Verbose "***** Post-publishing $current_config $takhub_rid TakHub."

                & "$script_dir/PostPublish.ps1" -Project TakHub -Config $current_config
                rm -Force -Verbose -Recurse TakHub -ErrorAction SilentlyContinue | Out-Null
                mv -Force -Verbose publish TakHub

                Write-Verbose "***** Building TakHub $current_config $takhub_rid zip file."

                $zip_file = $takhub_portable_zip;

                if ($takhub_rid)
                {
                    # Rename TakHub in $takhub_rid directory temporarily, otherwise 7z will include it in the zip file!
                    mv -Force -Verbose "$takhub_rid/TakHub" "$takhub_rid/TakHubX" -ErrorAction SilentlyContinue

                    $zip_file = if ($self_contained -eq "true") { $takhub_rid_standalone_bin_zip } `
                                                           else { $takhub_rid_dependent_bin_zip }
                    $zip_file = $zip_file -replace "RID", $takhub_rid
                }

                7z a -r -mx9 -bb2  "$zip_file" TakHub
                mv -Force -Verbose "$zip_file" $tak_dir

                if ($takhub_rid)
                {
                    # Revert file to original name.
                    mv -Force -Verbose "$takhub_rid/TakHubX" "$takhub_rid/TakHub" -ErrorAction SilentlyContinue
                }
            }
        }
    }

    popd
}
