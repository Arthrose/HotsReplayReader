echo %~dp0

dotnet clean
rmdir /s /q obj
rmdir /s /q bin
dotnet restore

cd %~dp0Heroes.Element
dotnet clean
rmdir /s /q obj
rmdir /s /q bin
dotnet restore

cd %~dp0Heroes.Icons
dotnet clean
rmdir /s /q obj
rmdir /s /q bin
dotnet restore

cd %~dp0Heroes.StormReplayParser
dotnet clean
rmdir /s /q obj
rmdir /s /q bin
dotnet restore

cd %~dp0HotsReplayReader.Updater
dotnet clean
rmdir /s /q obj
rmdir /s /q bin
dotnet restore

cd %~dp0
dotnet restore
