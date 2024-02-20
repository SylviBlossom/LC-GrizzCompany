:: Variables sent from compiler
set TargetDir=%~1
set TargetName=%~2
set ProjectDir=%~3

set BaseDir=%ProjectDir%..\

:: Make sure the package directory exists
mkdir "%BaseDir%package"

:: Copy all files needed for the package to the package directory
copy /Y "%TargetDir%%TargetName%.dll" "%BaseDir%package\%TargetName%.dll"
copy /Y "%BaseDir%assets\grizzcompany-enemies" "%BaseDir%package\grizzcompany-enemies.bundle"
copy /Y "%BaseDir%manifest.json" "%BaseDir%package\manifest.json"
copy /Y "%BaseDir%README.md" "%BaseDir%package\README.md"
copy /Y "%BaseDir%CHANGELOG.md" "%BaseDir%package\CHANGELOG.md"
copy /Y "%BaseDir%icon.png" "%BaseDir%package\icon.png"
