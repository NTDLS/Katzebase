[Setup]
;-- Main Setup Information
 AppName                          = Katzebase Server
 AppVerName                       = Katzebase Server 0.32.0
 AppCopyright                     = Copyright © 2025 NetworkDLS.
 DefaultDirName                   = {commonpf}\NetworkDLS\Katzebase
 DefaultGroupName                 = Katzebase
 UninstallDisplayIcon             = {app}\NTDLS.Katzebase.Server.exe
 SetupIconFile                    = "..\@Art\Icon.ico"
 PrivilegesRequired               = admin
 Uninstallable                    = Yes
 MinVersion                       = 0.0,7.0
 Compression                      = bZIP/9
 ChangesAssociations              = Yes
 OutputBaseFilename               = Katzebase.Windows.Bundle.x64
 ArchitecturesInstallIn64BitMode  = x64compatible
 AppPublisher                     = NetworkDLS
 AppPublisherURL                  = http://www.NetworkDLS.com/
 AppUpdatesURL                    = http://www.NetworkDLS.com/
 AppVersion                       = 0.32.0

[Components]
 Name: "Server";       Description: "Server";                     Types: Full Compact Custom;
 Name: "Management";   Description: "Management";                 Types: Full Compact Custom;
 Name: "Migration";    Description: "SQL Server Migration Tool";  Types: Full Custom;
  
 
[Files]
;Server component.
 Source: "publish\win-x64\Server\*.*"; DestDir: "{app}\Server"; Flags: IgnoreVersion recursesubdirs; Components: Server;
 ;Management component.
 Source: "publish\win-x64\Management\*.*"; DestDir: "{app}\Management"; Flags: IgnoreVersion  recursesubdirs; Components: Management;
 Source: "..\NTDLS.Katzebase.Management\Fonts\CascadiaCodeBold.ttf"; DestDir: "{fonts}"; FontInstall: "CascadiaCode Bold"; Flags: onlyifdoesntexist uninsneveruninstall
 Source: "..\NTDLS.Katzebase.Management\Fonts\CascadiaCodeExtraLight.ttf"; DestDir: "{fonts}"; FontInstall: "CascadiaCode Extra-Light"; Flags: onlyifdoesntexist uninsneveruninstall
 Source: "..\NTDLS.Katzebase.Management\Fonts\CascadiaCodeLight.ttf"; DestDir: "{fonts}"; FontInstall: "CascadiaCode Light"; Flags: onlyifdoesntexist uninsneveruninstall
 Source: "..\NTDLS.Katzebase.Management\Fonts\CascadiaCodeRegulat.ttf"; DestDir: "{fonts}"; FontInstall: "CascadiaCode Regulat"; Flags: onlyifdoesntexist uninsneveruninstall
 Source: "..\NTDLS.Katzebase.Management\Fonts\CascadiaCodeSemiBold.ttf"; DestDir: "{fonts}"; FontInstall: "CascadiaCode Semi-Bold"; Flags: onlyifdoesntexist uninsneveruninstall
 Source: "..\NTDLS.Katzebase.Management\Fonts\CascadiaCodeSemiLight.ttf"; DestDir: "{fonts}"; FontInstall: "CascadiaCode Semi-Light"; Flags: onlyifdoesntexist uninsneveruninstall
 Source: "..\@Art\Script.ico"; DestDir: "{app}\Management"; Flags: IgnoreVersion; Components: Management;
 
;Migration component.
 Source: "publish\win-x64\SQLServerMigration\*.*"; DestDir: "{app}\Migration"; Flags: IgnoreVersion  recursesubdirs; Components: Migration;
 
[Icons]
;Management component.
 Name: "{group}\Katzebase Manager"; Filename: "{app}\Management\NTDLS.Katzebase.Management.exe"; Components: Management;
 Name: "{commondesktop}\Katzebase Manager"; Filename: "{app}\Management\NTDLS.Katzebase.Management.exe"; Components: Management;
 Name: "{userdesktop}\Katzebase Manager"; Filename: "{app}\Management\NTDLS.Katzebase.Management.exe"; Components: Management;
;Migration component.
 Name: "{group}\Katzebase SQL Server Migration";  Filename: "{app}\Migration\NTDLS.Katzebase.SQLServerMigration.exe"; Components: Migration;
 
[Registry]
;-- Create kbs "Katzebase SQL Script" Association for management component.
 Root: HKCR; Subkey: ".kbs"; Flags: uninsdeletekey; Components: Management;
 Root: HKCR; Subkey: ".kbs"; ValueType: string; ValueData: "KatzebaseScript"; Components: Management;
 Root: HKCR; Subkey: "KatzebaseScript"; Flags: uninsdeletekey; Components: Management;
 Root: HKCR; Subkey: "KatzebaseScript"; ValueType: string; ValueData:"Katzebase Script"; Components: Management;
 Root: HKCR; Subkey: "KatzebaseScript\Shell"; ValueType: string; ValueData:"Open"; Components: Management;
 Root: HKCR; Subkey: "KatzebaseScript\Shell\Open\Command"; Flags: uninsdeletekey; Components: Management;
 Root: HKCR; Subkey: "KatzebaseScript\DefaultIcon"; ValueType: string; ValueData:"{app}\NTDLS.Katzebase.Management.exe,1"; Components: Management; 
 Root: HKCR; Subkey: "KatzebaseScript\DefaultIcon"; Flags: uninsdeletekey; Components: Management;
 Root: HKCR; Subkey: "KatzebaseScript\DefaultIcon"; ValueType: string; ValueData:"{app}\Management\Script.ico"; Components: Management;

[Run]
;Management component.
 Filename: "{app}\Management\NTDLS.Katzebase.Management.exe"; Description: "Run Katzebase Manager now?"; Flags: postinstall nowait skipifsilent shellexec; Components: Management;
;Server component.
 Filename: "{app}\Server\NTDLS.Katzebase.Server.exe"; Parameters: "install"; Flags: runhidden; StatusMsg: "Installing services..."; Components: Server;
 Filename: "{app}\Server\NTDLS.Katzebase.Server.exe"; Parameters: "start"; Flags: runhidden; StatusMsg: "Starting services..."; Components: Server;
;Migration component.
;Filename: "{app}\NTDLS.Katzebase.SQLServerMigration.exe"; Description: "Run SQL Server Migration now?"; Flags: postinstall nowait skipifsilent shellexec; Components: Management;

[UninstallRun]
 Filename: "{app}\Server\NTDLS.Katzebase.Server.exe"; Parameters: "uninstall"; Flags: runhidden; StatusMsg: "Removing services..."; RunOnceId: "ServiceRemoval"; Components: Server;
 
