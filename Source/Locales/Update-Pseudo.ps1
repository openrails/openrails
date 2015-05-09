function Get-Translation {
    process {
        $a = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz"
        $b = "ÂßÇÐÉFGHÌJK£MNÓÞQR§TÛVWXÝZáβçδèƒϱλïJƙℓ₥ñôƥ9řƨƭúƲωж¥ƺ"
        Write-Output ('[{0} !!!]' -f ((($_ -split '' | % {
            $inFormat = 0
        } {
            $i = $a.IndexOf($_);
            if (-not $inFormat -and $_ -and $i -ge 0) { Write-Output $b[$i] } else { Write-Output $_ }
            if (-not $inFormat -and $_ -eq '{') { $inFormat = 1 }
            elseif ($inFormat -and $_ -eq '{') { $inFormat = 0 }
            elseif ($inFormat -and $_ -eq '}') { $inFormat = 0 }
        }) -join '') -creplace '\\ř','\r' -creplace '\\ñ','\n'))
    }
}

gci -Directory | %{
    $file = $_
    Write-Host ('Reading template file ''{0}''' -f (gi ($file.Name + '\*.pot')))
    (gc -Encoding UTF8 ($file.Name + '\*.pot') | %{
        $header = 1
        $msgid = @()
        $msgid_plural = @()
    } {
        if ($header -and $_ -cmatch '^#:') {
            $header = 0
            Write-Output $_
        } elseif ($header -and $_ -like '"Project-Id-Version: *"') {
            Write-Output ('"Project-Id-Version: {0}\n"' -f $file.Name)
        } elseif ($header -and $_ -like '"Language-Team: *"') {
            Write-Output '"Language-Team: Open Rails Dev Team\n"'
            Write-Output '"Language: qps-ploc\n"'
        } elseif ($header -and $_ -like '"Language: *"') {
        } elseif ($header -and $_ -like '"X-Generator: *"') {
            Write-Output '"X-Generator: PowerShell Update-Pseudo.ps1\n"'
            Write-Output '"Plural-Forms: nplurals=2; plural=(n != 1);\n"'
        } elseif ($header) {
            Write-Output $_
        } elseif ($_ -cmatch '^msgid "(.*)"') {
            $msgid = @($Matches[1])
            Write-Output $_
        } elseif ($_ -cmatch '^msgid_plural "(.*)"') {
            $msgid_plural = @($Matches[1])
            Write-Output $_
        } elseif ($msgid.Length -gt 0 -and $msgid_plural.Length -eq 0 -and $_ -cmatch '^"(.*)"$') {
            $msgid += @($Matches[1])
            Write-Output $_
        } elseif ($msgid_plural.Length -gt 0 -and $_ -cmatch '^"(.*)"$') {
            $msgid_plural += @($Matches[1])
            Write-Output $_
        } elseif ($msgid.Length -gt 0 -and $_ -cmatch '^msgstr ""') {
            if ($msgid.Length -gt 1) {
                Write-Output 'msgstr ""'
                ((($msgid | select -Skip 1) -join "`n") | Get-Translation) -split "`n" | %{'"{0}"' -f $_}
            } else {
                $msgid[0] | Get-Translation | %{'msgstr "{0}"' -f $_}
            }
            $msgid = @()
        } elseif ($msgid.Length -gt 0 -and $_ -cmatch '^msgstr\[0\] ""') {
            if ($msgid.Length -gt 1) {
                Write-Output 'msgstr[0] ""'
                ((($msgid | select -Skip 1) -join "`n") | Get-Translation) -split "`n" | %{'"{0}"' -f $_}
            } else {
                $msgid[0] | Get-Translation | %{'msgstr[0] "{0}"' -f $_}
            }
            $msgid = @()
        } elseif ($msgid_plural.Length -gt 0 -and $_ -cmatch '^msgstr\[1\] ""') {
            if ($msgid_plural.Length -gt 1) {
                Write-Output 'msgstr[1] ""'
                ((($msgid_plural | select -Skip 1) -join "`n") | Get-Translation) -split "`n" | %{'"{0}"' -f $_}
            } else {
                $msgid_plural[0] | Get-Translation | %{'msgstr[1] "{0}"' -f $_}
            }
            $msgid_plural = @()
        } elseif ($_ -like '"Project-Id-Version: *"') {
            Write-Output ('"Project-Id-Version: {0}\n"' -f $file.Name)
        } elseif ($_ -like '"Language-Team: *"') {
            Write-Output '"Language-Team: Open Rails Dev Team\n"'
            Write-Output '"Language: qps-ploc\n"'
        } elseif ($_ -like '"Language: *"') {
        } elseif ($_ -like '"X-Generator: *"') {
            Write-Output '"X-Generator: PowerShell Update-Pseudo.ps1\n"'
            Write-Output '"Plural-Forms: nplurals=2; plural=(n != 1);\n"'
        } else {
            Write-Output $_
        }
    }) -join "`r`n" | % {
        [System.IO.File]::WriteAllLines(($file.Name + '\qps-ploc.po'), $_)
    }
}