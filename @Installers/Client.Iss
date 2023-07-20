[Setup]
;-- Main Setup Information
 AppName                          = Katzebase Client
 AppVerName                       = Katzebase Client 1.0.0.0
 AppCopyright                     = Copyright © 1995-2023 NetworkDLS.
 DefaultDirName                   = {commonpf}\NetworkDLS\Katzebase\UI
 DefaultGroupName                 = NetworkDLS\Katzebase
 UninstallDisplayIcon             = {app}\Katzebase.UI.exe
 SetupIconFile                    = "../@Art\Catzebase.ico"
 PrivilegesRequired               = admin
 Uninstallable                    = Yes
 MinVersion                       = 0.0,7.0
 Compression                      = bZIP/9
 ChangesAssociations              = Yes
 OutputBaseFilename               = Katzebase_Client
 ArchitecturesInstallIn64BitMode  = x64

;-- Windows 2000 & XP (Support Dialog)
 AppPublisher    = NetworkDLS
 AppPublisherURL = http://www.NetworkDLS.com/
 AppUpdatesURL   = http://www.NetworkDLS.com/
 AppVersion      = 1.0.0.0

[Files]
 Source: "..\Katzebase.UI\bin\Release\net7.0-windows\*.exe";  DestDir: "{app}";                  Flags: IgnoreVersion;
 Source: "..\Katzebase.UI\bin\Release\net7.0-windows\*.dll";  DestDir: "{app}";                  Flags: IgnoreVersion;
 Source: "..\Katzebase.UI\bin\Release\net7.0-windows\*.json"; DestDir: "{app}";                  Flags: IgnoreVersion;
 Source: ".\Highlighters\*.*";                                DestDir: "{app}\Highlighters";     Flags: IgnoreVersion;

[Icons]
 Name: "{group}\Katzebase Manager";          Filename: "{app}\Katzebase.UI.exe";
 Name: "{commondesktop}\Katzebase Manager";  Filename: "{app}\Katzebase.UI.exe";
 Name: "{userdesktop}\Katzebase Manager";    Filename: "{app}\Katzebase.UI.exe";

[Registry]
;-- Create SS "Katzebase SQL Script" Association
 Root: HKCR; Subkey: ".kbs"; Flags: uninsdeletekey;
 Root: HKCR; Subkey: ".kbs"; ValueType: string; ValueData: "KatzebaseScript";
 Root: HKCR; Subkey: "KatzebaseScript"; Flags: uninsdeletekey;
 Root: HKCR; Subkey: "KatzebaseScript"; ValueType: string; ValueData:"Katzebase Script";
 Root: HKCR; Subkey: "KatzebaseScript\Shell"; ValueType: string; ValueData:"Open";
 Root: HKCR; Subkey: "KatzebaseScript\Shell\Open\Command"; Flags: uninsdeletekey; 
 Root: HKCR; Subkey: "KatzebaseScript\Shell\Open\Command"; ValueType: string; ValueData:"""{app}\Katzebase.UI.exe"" ""%1""";
 Root: HKCR; Subkey: "KatzebaseScript\DefaultIcon"; Flags: uninsdeletekey;
 Root: HKCR; Subkey: "KatzebaseScript\DefaultIcon"; ValueType: string; ValueData:"{app}\Katzebase.UI.exe,1";

 Root: HKLM; Subkey: "Software\NetworkDLS\Katzebase\UI";                                                             Flags: uninsdeletekey noerror;
 Root: HKLM; Subkey: "Software\NetworkDLS\Katzebase\UI\"; ValueType: string; ValueName: "Path";   ValueData:"{app}"; Flags: CreateValueIfDoesntExist;

;[Tasks]
; Name: DesktopIcon;        Description: "Create a &desktop icon"; GroupDescription: "Additional icons:"; Components: SSUI
; Name: DesktopIcon\Common; Description: "For all users"; GroupDescription: "Additional icons:"; Components: SSUI; Flags: exclusive
; Name: DesktopIcon\User;   Description: "For the current user only"; GroupDescription: "Additional icons:"; Components: SSUI; Flags: exclusive unchecked

;[Run]
; Filename: "{app}\UI\SSUI.exe"; Description: "Run UI now?"; Flags: postinstall nowait skipifsilent shellexec; Components: SSUI;
