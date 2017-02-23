#requires -version 4
param(
    [switch]$Confirm,
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]] $GitArgs
)

if ($args.Length -gt 0 -and $args[0] -eq 'clean' -and !$Confirm) {
    Write-Warning 'Detected a clean operation, which may  remove working changes.'
    Write-Warning 'Re-run with -Confirm to proceed'
    exit 1
}

$repos = Get-Content "$PSScriptRoot/repos.txt" | ? { $_ -notlike '`#*' -and ($_)  }

function do_git($repoName) {
    Push-Location "$PSScriptRoot/$repoName"
    try {
        Write-Host $repoName -ForegroundColor Yellow
        & git @GitArgs
    }
    finally {
        Pop-Location
    }
}

$repos | % { do_git $_ }

