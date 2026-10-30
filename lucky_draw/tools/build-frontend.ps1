# Build giao diện cho máy cấu hình thấp (chỉ cần chạy lại khi sửa class Tailwind trong các view):
#   1. Sinh file CSS Tailwind tĩnh (wwwroot/css/tailwind.css) thay cho tailwindcss.js dịch CSS trong trình duyệt
#   2. Tải Vue bản production (wwwroot/lib/vue/vue.global.prod.js)
# Cách chạy (PowerShell, tại thư mục lucky_draw):  powershell -ExecutionPolicy Bypass -File tools\build-frontend.ps1
# Sau khi chạy xong, khởi động lại ứng dụng để dùng các file mới.
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

$tailwindVersion = '3.4.17' # Cùng phiên bản với wwwroot/lib/tailwind/tailwindcss.js
$vueVersion = '3.5.30'      # Cùng phiên bản với wwwroot/lib/vue/vue.global.js

$projectDir = Split-Path -Parent $PSScriptRoot
$toolDir = Join-Path $PSScriptRoot 'tailwind'
$cliPath = Join-Path $env:TEMP "tailwindcss-$tailwindVersion.exe"

if (-not (Test-Path $cliPath)) {
    Write-Host "Tải Tailwind CLI $tailwindVersion..."
    Invoke-WebRequest "https://github.com/tailwindlabs/tailwindcss/releases/download/v$tailwindVersion/tailwindcss-windows-x64.exe" -OutFile $cliPath
}

Write-Host 'Build wwwroot/css/tailwind.css...'
New-Item -ItemType Directory -Force (Join-Path $projectDir 'wwwroot\css') | Out-Null
& $cliPath -c (Join-Path $toolDir 'tailwind.config.js') -i (Join-Path $toolDir 'input.css') -o (Join-Path $projectDir 'wwwroot\css\tailwind.css') --minify
if ($LASTEXITCODE -ne 0) { throw 'Build Tailwind thất bại' }

Write-Host "Tải Vue $vueVersion production..."
Invoke-WebRequest "https://cdn.jsdelivr.net/npm/vue@$vueVersion/dist/vue.global.prod.js" -OutFile (Join-Path $projectDir 'wwwroot\lib\vue\vue.global.prod.js')

Write-Host 'Xong. Khởi động lại ứng dụng để áp dụng.'
