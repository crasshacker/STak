#
# ApplyGitHubStyles.ps1 - Wrap the contents of an HTML file in a wrapper that applies
#                         GitHub styles to it, producing a new HTML file.
#
# See https://github.com/sindresorhus/github-markdown-css.
#
if ($args.Length -lt 1 -or $args.Length -gt 2)
{
    throw "Usage: ApplyGitHubStyles.ps1 <html_file> [<output_file>]"
}

$html_file = $args[0]
$out_file  = $args[1]

$header = @"
<meta name="viewport" content="width=device-width, initial-scale=1">
<link rel="stylesheet" href="github-markdown.css">
<style>
	.markdown-body {
		box-sizing: border-box;
		min-width: 200px;
		max-width: 980px;
		margin: 0 auto;
		padding: 45px;
	}

        .center {
                text-align:center;
        }

	@media (max-width: 767px) {
		.markdown-body {
			padding: 15px;
		}
	}
</style>
<article class="markdown-body">
"@

$footer = "</article>"

if ($out_file)
{
    Write-Output $header    > $out_file
    Get-Content $html_file >> $out_file
    Write-Output $footer   >> $out_file
}
else
{
    Write-Output $header
    Get-Content $html_file
    Write-Output $footer
}
