# Windows 11 installation and local setup

## Install official tools in this order
1. VS Code: https://code.visualstudio.com/download
2. Git: https://git-scm.com/downloads
3. Python 3.12: https://www.python.org/downloads/
4. AWS CLI v2: https://docs.aws.amazon.com/cli/latest/userguide/getting-started-install.html
5. Docker Desktop (WSL2): https://docs.docker.com/desktop/setup/install/windows-install/
6. OWASP Threat Dragon: https://www.threatdragon.com/
7. Burp Suite Community: https://portswigger.net/burp/communitydownload
8. Later: Semgrep, Trivy, Checkov, OWASP ZAP and Garak (follow official docs).

## Verify in PowerShell
```powershell
code --version
git --version
py -3.12 --version
aws --version
docker --version
```

## Create local workspace
```powershell
mkdir C:\Projects\ContractGuard
# Unzip kit, rename root directory to C:\Projects\ContractGuard\security
cd C:\Projects\ContractGuard\security
py -3.12 -m venv .venv
.\.venv\Scripts\python.exe -m pip install -r requirements.txt
.\.venv\Scripts\python.exe -m pytest -q
.\.venv\Scripts\python.exe -m src.rule_validator --input fixtures/mixed.json --output out/security-findings.json
```

## Setup AWS CLI SSO (only after account owner grants access)
```powershell
aws configure sso --profile contractguard-dev
aws sso login --profile contractguard-dev
aws sts get-caller-identity --profile contractguard-dev
```
Do not put access keys, secret keys or tokens in the repo; use assigned permission set, ideally dev environment only. Set AWS Budgets before starting billable services.

## Docker (later)
Run `docker info` and make sure Docker Desktop starts. Linux containers via WSL2 are preferred for image scanning. If unavailable on this PC, rule tests still run without Docker.
