param(
  [Parameter(Mandatory = $false)]
  [string]$Url = "ws://localhost:8085/",

  [Parameter(Mandatory = $false)]
  [switch]$SendReq,

  [Parameter(Mandatory = $false)]
  [switch]$SendCount,

  [Parameter(Mandatory = $false)]
  [string]$ReqId = "req_probe",

  [Parameter(Mandatory = $false)]
  [string]$CountId = "count_probe",

  # Either a single filter object JSON, or a JSON array of filter objects.
  [Parameter(Mandatory = $false)]
  [string]$ReqFiltersJson = '{ "kinds": [1], "limit": 1 }',

  # Either a single filter object JSON, or a JSON array of filter objects.
  [Parameter(Mandatory = $false)]
  [string]$CountFiltersJson = '{ "kinds": [1] }',

  [Parameter(Mandatory = $false)]
  [int]$TimeoutSeconds = 10
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Parse-FiltersJson([string]$json) {
  $obj = $json | ConvertFrom-Json

  if ($null -eq $obj) {
    return @()
  }

  if ($obj -is [System.Array]) {
    return @($obj)
  }

  return @($obj)
}

function Send-JsonArray([System.Net.WebSockets.ClientWebSocket]$ws, [object[]]$message) {
  $json = $message | ConvertTo-Json -Compress -Depth 100
  $bytes = [System.Text.Encoding]::UTF8.GetBytes($json)
  $segment = [System.ArraySegment[byte]]::new($bytes)
  $ws.SendAsync($segment, [System.Net.WebSockets.WebSocketMessageType]::Text, $true, [System.Threading.CancellationToken]::None).GetAwaiter().GetResult()
  Write-Host ">> $json"
}

Write-Host "Connecting: $Url"

$ws = [System.Net.WebSockets.ClientWebSocket]::new()
$ws.Options.KeepAliveInterval = [TimeSpan]::FromSeconds(20)
$ws.ConnectAsync([Uri]$Url, [System.Threading.CancellationToken]::None).GetAwaiter().GetResult()

try {
  if (-not $SendReq -and -not $SendCount) {
    $SendReq = $true
    $SendCount = $true
  }

  $expectedDone = New-Object 'System.Collections.Generic.HashSet[string]'

  if ($SendReq) {
    $reqFilters = Parse-FiltersJson $ReqFiltersJson
    $msg = @("REQ", $ReqId) + $reqFilters
    $expectedDone.Add("REQ:$ReqId") | Out-Null
    Send-JsonArray $ws $msg
  }

  if ($SendCount) {
    $countFilters = Parse-FiltersJson $CountFiltersJson
    $msg = @("COUNT", $CountId) + $countFilters
    $expectedDone.Add("COUNT:$CountId") | Out-Null
    Send-JsonArray $ws $msg
  }

  $done = New-Object 'System.Collections.Generic.HashSet[string]'
  $sw = [System.Diagnostics.Stopwatch]::StartNew()

  while ($sw.Elapsed.TotalSeconds -lt $TimeoutSeconds -and $ws.State -eq [System.Net.WebSockets.WebSocketState]::Open) {
    $buffer = New-Object byte[] 65536
    $ms = New-Object System.IO.MemoryStream

    while ($true) {
      $seg = [System.ArraySegment[byte]]::new($buffer)
      $result = $ws.ReceiveAsync($seg, [System.Threading.CancellationToken]::None).GetAwaiter().GetResult()

      if ($result.MessageType -eq [System.Net.WebSockets.WebSocketMessageType]::Close) {
        Write-Host "<< [CLOSE] $($result.CloseStatus) $($result.CloseStatusDescription)"
        break
      }

      $ms.Write($buffer, 0, $result.Count)

      if ($result.EndOfMessage) {
        break
      }
    }

    if ($ms.Length -eq 0) {
      continue
    }

    $json = [System.Text.Encoding]::UTF8.GetString($ms.ToArray())
    Write-Host "<< $json"

    try {
      $msg = $json | ConvertFrom-Json
      if ($msg -isnot [System.Array] -or $msg.Count -lt 1) {
        continue
      }

      $type = [string]$msg[0]
      $id = if ($msg.Count -ge 2) { [string]$msg[1] } else { "" }

      switch ($type) {
        "EOSE"   { $done.Add("REQ:$id")    | Out-Null }
        "CLOSED" { $done.Add("REQ:$id")    | Out-Null; $done.Add("COUNT:$id") | Out-Null }
        "COUNT"  { $done.Add("COUNT:$id")  | Out-Null }
        default  { }
      }

      if ($expectedDone.Count -gt 0 -and $done.IsSupersetOf($expectedDone)) {
        break
      }
    }
    catch {
      # Ignore JSON parse errors and keep printing raw frames.
    }
  }
}
finally {
  if ($ws.State -eq [System.Net.WebSockets.WebSocketState]::Open) {
    $ws.CloseAsync([System.Net.WebSockets.WebSocketCloseStatus]::NormalClosure, "bye", [System.Threading.CancellationToken]::None).GetAwaiter().GetResult()
  }
  $ws.Dispose()
}

