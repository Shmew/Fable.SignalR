@echo off
cls

dotnet restore build.proj

fake build %*