@echo off
dotnet clean -c Release -v minimal
dotnet clean -c Release -v minimal -r win-x64
dotnet publish ApiDump -c Release -r win-x64 --force --no-self-contained -p:PublishSingleFile=true
