[Setup]
;-- Main Setup Information
 AppName                          = Katzebase Client
 AppVerName                       = Katzebase Client 1.0.5.0
 AppCopyright                     = Copyright © 1995-2023 NetworkDLS.
 DefaultDirName                   = {commonpf}\NetworkDLS\Katzebase\UI
 DefaultGroupName                 = NetworkDLS\Katzebase
 UninstallDisplayIcon             = {app}\NTDLS.Katzebase.UI.exe
 SetupIconFile                    = "..\@Art\Katzebase.ico"
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
 AppVersion      = 1.0.5.0

[Components]
 Name: "Client";         Description: "Client";                     Types: Full Compact Custom; Flags: Fixed;
 Name: "MSSQLMigrate";   Description: "SQL Server Migration Tool";  Types: Full Custom;
 

[Files]
 Source: "..\NTDLS.Katzebase.UI\bin\Release\net7.0-windows\*.exe";        DestDir: "{app}";                       Flags: IgnoreVersion;                Components: Client; 
 Source: "..\NTDLS.Katzebase.UI\bin\Release\net7.0-windows\*.dll";        DestDir: "{app}";                       Flags: IgnoreVersion;                Components: Client; 
 Source: "..\NTDLS.Katzebase.UI\bin\Release\net7.0-windows\*.json";       DestDir: "{app}";                       Flags: IgnoreVersion;                Components: Client;
 Source: "..\NTDLS.Katzebase.UI\bin\Release\net7.0-windows\Runtimes\*.*"; DestDir: "{app}\MSSQLMigrate\Runtimes"; Flags: IgnoreVersion recursesubdirs; Components: Client;
 Source: ".\Highlighters\*.*";                                            DestDir: "{app}\Highlighters";          Flags: IgnoreVersion;                Components: Client;

 Source: "..\NTDLS.Katzebase.SQLServerMigration\bin\Release\net7.0-windows\*.exe";        DestDir: "{app}\MSSQLMigrate";          Flags: IgnoreVersion;                Components: MSSQLMigrate;
 Source: "..\NTDLS.Katzebase.SQLServerMigration\bin\Release\net7.0-windows\*.dll";        DestDir: "{app}\MSSQLMigrate";          Flags: IgnoreVersion;                Components: MSSQLMigrate;
 Source: "..\NTDLS.Katzebase.SQLServerMigration\bin\Release\net7.0-windows\*.json";       DestDir: "{app}\MSSQLMigrate";          Flags: IgnoreVersion;                Components: MSSQLMigrate;
 Source: "..\NTDLS.Katzebase.SQLServerMigration\bin\Release\net7.0-windows\Runtimes\*.*"; DestDir: "{app}\MSSQLMigrate\Runtimes"; Flags: IgnoreVersion recursesubdirs; Components: MSSQLMigrate;

[Icons]
 Name: "{group}\SQL Server Migration Tool";  Filename: "{app}\MSSQLMigrate\NTDLS.Katzebase.SQLServerMigration.exe"; Components: Client; 
 Name: "{group}\Katzebase Manager";          Filename: "{app}\NTDLS.Katzebase.UI.exe";                              Components: Client; 
 Name: "{commondesktop}\Katzebase Manager";  Filename: "{app}\NTDLS.Katzebase.UI.exe";                              Components: Client; 
 Name: "{userdesktop}\Katzebase Manager";    Filename: "{app}\NTDLS.Katzebase.UI.exe";                              Components: Client; 

[Registry]
;-- Create SS "Katzebase SQL Script" Association
 Root: HKCR; Subkey: ".kbs"; Flags: uninsdeletekey;
 Root: HKCR; Subkey: ".kbs"; ValueType: string; ValueData: "KatzebaseScript";
 Root: HKCR; Subkey: "KatzebaseScript"; Flags: uninsdeletekey;
 Root: HKCR; Subkey: "KatzebaseScript"; ValueType: string; ValueData:"Katzebase Script";
 Root: HKCR; Subkey: "KatzebaseScript\Shell"; ValueType: string; ValueData:"Open";
 Root: HKCR; Subkey: "KatzebaseScript\Shell\Open\Command"; Flags: uninsdeletekey; 
 Root: HKCR; Subkey: "KatzebaseScript\Shell\Open\Command"; ValueType: string; ValueData:"""{app}\NTDLS.Katzebase.UI.exe"" ""%1""";
 Root: HKCR; Subkey: "KatzebaseScript\DefaultIcon"; Flags: uninsdeletekey;
 Root: HKCR; Subkey: "KatzebaseScript\DefaultIcon"; ValueType: string; ValueData:"{app}\Katzebase.UI.exe,1";

 Root: HKLM; Subkey: "Software\NetworkDLS\Katzebase\UI";                                                             Flags: uninsdeletekey noerror;
 Root: HKLM; Subkey: "Software\NetworkDLS\Katzebase\UI\"; ValueType: string; ValueName: "Path";   ValueData:"{app}"; Flags: CreateValueIfDoesntExist;

[Run]
 Filename: "{app}\NTDLS.Katzebase.UI.exe"; Description: "Run Katzebase UI now?"; Flags: postinstall nowait skipifsilent shellexec;
