@echo off

where git >NUL
if NOT ["%errorlevel%"]==["0"] (
	echo git not found on PATH
    pause
    exit /b %errorlevel%
)

echo Restoring git submodules
git submodule update --init --recursive
if NOT ["%errorlevel%"]==["0"] (
    pause
    exit /b %errorlevel%
)

where dotnet >NUL
if NOT ["%errorlevel%"]==["0"] (
	echo dotnet not found on PATH. Install .NET Core!
    pause
    exit /b %errorlevel%
)

If "%1"=="auto" (
	echo building src/setup/setupAuto.csproj
	dotnet build src/setup/setupAuto.csproj --output "src/setup/bin/Debug/net6.0-windows"

	if NOT ["%errorlevel%"]==["0"] (
		pause
		exit /b %errorlevel%
	)

	"src/setup/bin/Debug/net6.0-windows/setupAuto.exe" %2
) Else (
	echo building src/setup/setup.csproj
	dotnet build src/setup/setup.csproj --output "src/setup/bin/Debug/net6.0-windows"

	if NOT ["%errorlevel%"]==["0"] (
		pause
		exit /b %errorlevel%
	)

	start "" "src/setup/bin/Debug/net6.0-windows/setup.exe" %*
)