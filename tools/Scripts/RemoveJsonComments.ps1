param
(
    [Parameter(Mandatory=$true)]
    [string[]] $InputPath,

    [Parameter(Mandatory=$false)]
    [string] $OutputPath,

    [Parameter(Mandatory=$false)]
    [switch] $Force
)

if ($OutputPath -and -not (Test-Path -PathType Container -Path $OutputPath))
{
    Write-Error "If specified, the OutputPath must be an existing directory."
    Write-Error "$OutputPath either does not exist or is not a directory."
    exit 1
}

foreach ($text_file in $InputPath)
{
    $json_file = $text_file -replace "\.[^.]*$", ".json"

    if ($OutputPath)
    {
        $json_file = [IO.Path]::Combine($OutputPath, [IO.Path]::GetFileName($json_file))
    }

    $text_info = ls $text_file -ErrorAction "SilentlyContinue" 2> $null
    $json_info = ls $json_file -ErrorAction "SilentlyContinue" 2> $null

    if (! $text_info)
    {
        Write-Error "File not found: $text_file"
        exit 1
    }

    if ($Force -or (! $json_info) -or ($text_info.LastWriteTime -gt $json_info.LastWriteTime))
    {
        Get-Content -Path $text_file -ReadCount 0 | where { $_ -notmatch '^$' } `
                                                  | where { $_ -notmatch '^\s*((#|//).*)$' } `
                                                  | %{ $_ -replace "\s+(#|//).*$", "" } > $json_file

        Write-Host "Converted $text_file to $json_file."
    }
    else
    {
        Write-Host "File update skipped; target file is not out of date with respect to source file $text_file."
    }
}
