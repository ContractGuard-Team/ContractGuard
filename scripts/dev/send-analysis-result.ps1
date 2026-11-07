<#
.SYNOPSIS
  Plays the AI worker's part for one job, against LocalStack only (no AWS, no cost).

.DESCRIPTION
  Until the real worker exists, this script sends the messages the worker would send to the
  analysis-results queue, and uploads the extraction result it would write to S3.
  Run from the repository root while `docker compose up` is running. The result it sends is the
  example in contracts/examples, not an analysis of the uploaded file.

  A job exists only when the API runs with Analysis__WorkerEnabled=true (compose: ANALYSIS_WORKER_ENABLED=true).
  Upload a contract and start its analysis in the web app first.

.EXAMPLE
  $job = docker compose exec -T postgres psql -U contractguard -d contractguard -At -c "SELECT analysis_run_id FROM analysis_run ORDER BY started_at DESC LIMIT 1;"
  ./scripts/dev/send-analysis-result.ps1 -JobId $job -Event progress -Step splitting_clauses
  ./scripts/dev/send-analysis-result.ps1 -JobId $job -Event completed
#>
param(
    [Parameter(Mandatory = $true)] [string] $JobId,
    [ValidateSet('progress', 'completed', 'failed', 'invalid', 'poison', 'pong')]
    [string] $Event = 'completed',
    [ValidateSet('reading_file', 'splitting_clauses', 'evaluating', 'scoring')]
    [string] $Step = 'splitting_clauses',
    [string] $Bucket = 'contractguard-local',
    [string] $Queue = 'analysis-results'
)

$ErrorActionPreference = 'Stop'

# Runs docker and fails on a non-zero exit code. Windows PowerShell 5.1 turns every stderr line of
# a native program into an error record, and 'Stop' would abort on progress messages that docker
# writes to stderr. So native calls run with 'Continue' and the exit code decides.
function Invoke-Docker([string[]] $Arguments) {
    $ErrorActionPreference = 'Continue'
    $output = docker @Arguments 2>$null
    if ($LASTEXITCODE -ne 0) { throw "docker $($Arguments -join ' ') failed with exit code $LASTEXITCODE" }
    return $output
}

# Runs the AWS CLI that ships inside the LocalStack container, so nothing is installed on the host.
function Invoke-LocalAws([string[]] $Arguments) {
    Invoke-Docker (@('compose', 'exec', '-T',
        '-e', 'AWS_ACCESS_KEY_ID=test', '-e', 'AWS_SECRET_ACCESS_KEY=test', '-e', 'AWS_DEFAULT_REGION=ap-southeast-1',
        'localstack', 'aws', '--endpoint-url', 'http://localhost:4566') + $Arguments)
}

# Copies text into the LocalStack container as a UTF-8 file without BOM and returns its path there.
# Two Windows PowerShell 5.1 pitfalls make this necessary:
#  - JSON passed as a command-line argument to a native program loses its double quotes;
#  - text piped to a native program always gets a UTF-8 BOM (bytes EF BB BF) in front.
function Copy-ToLocalStack([string] $Text, [string] $Name) {
    $tmp = Join-Path ([System.IO.Path]::GetTempPath()) $Name
    [System.IO.File]::WriteAllText($tmp, $Text, (New-Object System.Text.UTF8Encoding($false)))
    Invoke-Docker @('compose', 'cp', $tmp, "localstack:/tmp/$Name") | Out-Null
    Remove-Item $tmp
    return "/tmp/$Name"
}

function Send-ResultMessage([hashtable] $Body) {
    $json = $Body | ConvertTo-Json -Compress
    $queueUrl = (Invoke-LocalAws @('sqs', 'get-queue-url', '--queue-name', $Queue, '--query', 'QueueUrl', '--output', 'text')).Trim()
    $path = Copy-ToLocalStack $json 'cg-message.json'
    Invoke-LocalAws @('sqs', 'send-message', '--queue-url', $queueUrl, '--message-body', "file://$path") | Out-Null
    Write-Host "Sent to ${Queue}: $json"
}

switch ($Event) {
    'progress' {
        Send-ResultMessage @{ message_version = '1'; event_type = 'progress'; job_id = $JobId; step = $Step }
    }
    'failed' {
        Send-ResultMessage @{ message_version = '1'; event_type = 'failed'; job_id = $JobId; failure_reason = 'Simulated failure from send-analysis-result.ps1'; error_code = 'ERR-UPL-004' }
    }
    'pong' {
        Send-ResultMessage @{ message_version = '1'; event_type = 'pong'; job_id = $JobId }
    }
    'poison' {
        # No job_id: the API cannot handle it, leaves it on the queue, and after 3 receives
        # SQS moves it to analysis-results-dlq.
        Send-ResultMessage @{ message_version = '1'; event_type = 'completed' }
    }
    { $_ -in 'completed', 'invalid' } {
        # The worker copies trusted ids from the job request. Here they are read from the database.
        $sql = "SELECT r.contract_id, v.version_number FROM analysis_run r JOIN contract_version v ON v.contract_version_id = r.contract_version_id WHERE r.analysis_run_id = '$JobId';"
        $row = Invoke-Docker @('compose', 'exec', '-T', 'postgres', 'psql', '-U', 'contractguard', '-d', 'contractguard', '-tAc', $sql)
        if (-not $row) { throw "Job $JobId not found in analysis_run." }
        $contractId, $versionNumber = $row.Trim().Split('|')

        $examplePath = Join-Path $PSScriptRoot '..\..\contracts\examples\extraction.v2.example.json'
        $json = [System.IO.File]::ReadAllText((Resolve-Path $examplePath), [System.Text.Encoding]::UTF8)
        $json = $json.Replace('{{JOB_ID}}', $JobId).Replace('{{TENANT_ID}}', 'dev').Replace('{{CONTRACT_ID}}', $contractId).Replace('{{CONTRACT_VERSION}}', $versionNumber)
        if ($Event -eq 'invalid') {
            # Break the schema on purpose: "clauses" must be an array.
            $json = $json.Replace('"clauses": [', '"clauses": "not-an-array", "unused": [')
        }

        $key = "results/$JobId/extraction.json"
        $path = Copy-ToLocalStack $json 'cg-extraction.json'
        Invoke-LocalAws @('s3', 'cp', $path, "s3://$Bucket/$key", '--content-type', 'application/json') | Out-Null
        Write-Host "Uploaded s3://$Bucket/$key"
        Send-ResultMessage @{ message_version = '1'; event_type = 'completed'; job_id = $JobId; result_key = $key }
    }
}
