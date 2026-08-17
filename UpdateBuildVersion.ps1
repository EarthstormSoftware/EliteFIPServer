# Auto-increment build version script
# Format: MAJOR.MINOR.YYDDD.BUILD
# - YYDDD: Year and day of year
# - BUILD: Auto-increments, resets to 1 on new day

param(
    [string]$ProjectPath = "EliteFIPServer/EliteFIPServer.csproj"
)

# Get current date in YYDDD format (YY = last 2 digits of year, DDD = day of year)
$today = [DateTime]::Now
$year = $today.ToString("yy")
$dayOfYear = $today.DayOfYear.ToString("000")
$todayVersion = "$year$dayOfYear"

Write-Host "Build Version Update Script" -ForegroundColor Cyan
Write-Host "Today's date code: $todayVersion (Year: $year, Day: $dayOfYear)" -ForegroundColor Green

# Read the csproj file
$csprojContent = Get-Content $ProjectPath -Raw

# Extract current AssemblyVersion using regex
$versionMatch = [regex]::Match($csprojContent, '<AssemblyVersion>(\d+\.\d+\.\d+)\.(\d+)</AssemblyVersion>')

if ($versionMatch.Success) {
    $currentVersion = $versionMatch.Groups[1].Value
    $currentBuild = [int]$versionMatch.Groups[2].Value
    
    Write-Host "Current version: $currentVersion.$currentBuild" -ForegroundColor Yellow
    
    # Parse the current version to extract YYDDD portion
    $parts = $currentVersion.Split('.')
    $major = $parts[0]
    $minor = $parts[1]
    $currentDateCode = $parts[2]
    
    # Determine new build number
    if ($currentDateCode -eq $todayVersion) {
        # Same day - increment build number
        $newBuild = $currentBuild + 1
        Write-Host "Same day detected - incrementing build number: $currentBuild to $newBuild" -ForegroundColor Green
    }
    else {
        # New day - reset build number to 1
        $newBuild = 1
        Write-Host "New day detected ($currentDateCode to $todayVersion) - resetting build number to 1" -ForegroundColor Green
    }
    
    $newVersion = "$major.$minor.$todayVersion.$newBuild"
    Write-Host "New version: $newVersion" -ForegroundColor Cyan
    
    # Update AssemblyVersion
    $csprojContent = $csprojContent -replace `
        '<AssemblyVersion>(\d+\.\d+\.\d+)\.(\d+)</AssemblyVersion>', `
        "<AssemblyVersion>$newVersion</AssemblyVersion>"
    
    # Update FileVersion (same as AssemblyVersion)
    $csprojContent = $csprojContent -replace `
        '<FileVersion>(\d+\.\d+\.\d+)\.(\d+)</FileVersion>', `
        "<FileVersion>$newVersion</FileVersion>"
    
    # Write the updated content back
    Set-Content $ProjectPath $csprojContent
    
    Write-Host "Version updated successfully!" -ForegroundColor Green
    Write-Host "  AssemblyVersion: $newVersion" -ForegroundColor Green
    Write-Host "  FileVersion: $newVersion" -ForegroundColor Green
}
else {
    Write-Host "ERROR: Could not find AssemblyVersion in csproj file" -ForegroundColor Red
    exit 1
}
