[Setup]
;-- Main Setup Information
 AppName                          = Katzebase Server
 AppVerName                       = Katzebase Server 0.1.0.0
 AppCopyright                     = Copyright © 1995-2023 NetworkDLS.
 DefaultDirName                   = {commonpf}\NetworkDLS\Katzebase\Service
 DefaultGroupName                 = NetworkDLS\Katzebase
 UninstallDisplayIcon             = {app}\NTDLS.Katzebase.Service.exe
 SetupIconFile                    = "..\@Art\Katzebase.ico"
 PrivilegesRequired               = admin
 Uninstallable                    = Yes
 MinVersion                       = 0.0,7.0
 Compression                      = bZIP/9
 ChangesAssociations              = Yes
 OutputBaseFilename               = Katzebase_Service
 ArchitecturesInstallIn64BitMode  = x64

;-- Windows 2000 & XP (Support Dialog)
 AppPublisher    = NetworkDLS
 AppPublisherURL = http://www.NetworkDLS.com/
 AppUpdatesURL   = http://www.NetworkDLS.com/
 AppVersion      = 0.1.0.0

[Files]
 Source: "..\NTDLS.Katzebase.Service\bin\Release\net7.0\runtimes\*.*";        DestDir: "{app}\runtimes";              Flags: IgnoreVersion recursesubdirs;
 Source: "..\NTDLS.Katzebase.Service\bin\Release\net7.0\*.exe";               DestDir: "{app}";                       Flags: IgnoreVersion;
 Source: "..\NTDLS.Katzebase.Service\bin\Release\net7.0\*.dll";               DestDir: "{app}";                       Flags: IgnoreVersion;
 Source: "..\NTDLS.Katzebase.Service\bin\Release\net7.0\*.json";              DestDir: "{app}";                       Flags: IgnoreVersion;
 Source: "ServerSettings\*.*";                                                DestDir: "{app}";                       Flags: IgnoreVersion;

[Registry]
 Root: HKLM; Subkey: "Software\NetworkDLS\Katzebase\Service";                                                             Flags: uninsdeletekey noerror;
 Root: HKLM; Subkey: "Software\NetworkDLS\Katzebase\Service\"; ValueType: string; ValueName: "Path";   ValueData:"{app}"; Flags: CreateValueIfDoesntExist;

;[Tasks]
; Name: DesktopIcon;        Description: "Create a &desktop icon"; GroupDescription: "Additional icons:"; Components: SSUI
; Name: DesktopIcon\Common; Description: "For all users"; GroupDescription: "Additional icons:"; Components: SSUI; Flags: exclusive
; Name: DesktopIcon\User;   Description: "For the current user only"; GroupDescription: "Additional icons:"; Components: SSUI; Flags: exclusive unchecked

[Run]
 Filename: "{app}\NTDLS.Katzebase.Service.exe"; Parameters: "install"; Flags: runhidden; StatusMsg: "Installing service..."
 Filename: "{app}\NTDLS.Katzebase.Service.exe"; Parameters: "start"; Flags: runhidden; StatusMsg: "Starting service..."

[UninstallRun]
 Filename: "{app}\NTDLS.Katzebase.Service.exe"; Parameters: "uninstall"; Flags: runhidden; StatusMsg: "Installing service..."; RunOnceId: "ServiceRemoval"
 
