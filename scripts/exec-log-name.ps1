# P63 #430: editor methods must never share an evidence log merely because their
# terminal member name is the same (`Census`, `Gate`, and `ProveRed` are common).
# Keep this pure and deterministic so launchers and watchdogs resolve one path.
function Get-HeadlessExecLogName {
    param([Parameter(Mandatory = $true)][string]$Method)

    $full = $Method.Trim()
    if ([string]::IsNullOrWhiteSpace($full)) {
        throw "An editor method is required to name its evidence log"
    }
    $readable = $full -replace '^Lattice\.EditorTools\.', ''
    $readable = ($readable -replace '[^A-Za-z0-9]+', '-').Trim('-')
    if ($readable.Length -gt 96) { $readable = $readable.Substring(0, 96).TrimEnd('-') }

    $sha = [Security.Cryptography.SHA256]::Create()
    try {
        $bytes = [Text.Encoding]::UTF8.GetBytes($full)
        $hash = ([BitConverter]::ToString($sha.ComputeHash($bytes))).Replace('-', '').ToLowerInvariant()
    }
    finally { $sha.Dispose() }
    return "headless-exec-$readable-$($hash.Substring(0, 10)).log"
}
