@echo off
dotnet clean -c Release -v minimal
dotnet publish ApiDump -c Release --force
