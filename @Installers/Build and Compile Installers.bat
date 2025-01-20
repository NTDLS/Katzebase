@echo off
set path=%PATH%;C:\Program Files\Vroom Performance Technologies\SQL Script Generator;C:\Program Files\7-Zip;
set path=C:\Program Files (x86)\Inno Setup 6\;%path%

rd publish /q /s
rd output /q /s

md output
md publish

dotnet publish ..\NTDLS.Katzebase.Server -c Release -o publish\win-x64\Server --runtime win-x64 --self-contained false
del publish\win-x64\Server\*.pdb /q
dotnet publish ..\NTDLS.Katzebase.Management -c Release -o publish\win-x64\Management --runtime win-x64 --self-contained false
del publish\win-x64\Server\*.pdb /q
dotnet publish ..\NTDLS.Katzebase.SQLServerMigration -c Release -o publish\win-x64\SQLServerMigration --runtime win-x64 --self-contained false
del publish\win-x64\SQLServerMigration\*.pdb /q

dotnet publish ..\NTDLS.Katzebase.Server -c Release -o publish\linux-x64 --runtime linux-x64 --self-contained false
del publish\linux-x64\*.pdb /q

7z.exe a -tzip -r -mx9 ".\output\Katzebase.linux.x64.zip" ".\publish\linux-x64\*.*"

iscc Windows.Installer.iss
rd publish /q /s
