attrib ".vs" -h -a -r -s
attrib *.* -h -a -r -s

rd ".vs" /S /Q

rd "rd "@Installers\Output" /S /Q

rd "Katzebase.Engine\bin" /S /Q
rd "rd "Katzebase.Engine\obj" /S /Q
rd "Katzebase.Shared\bin" /S /Q
rd "rd "Katzebase.Shared\obj" /S /Q
rd "Katzebase\bin" /S /Q
rd "rd "Katzebase\obj" /S /Q
rd "Katzebase.Service\bin" /S /Q
rd "rd "Katzebase.Service\obj" /S /Q
rd "Katzebase.TestHarness\bin" /S /Q
rd "rd "Katzebase.TestHarness\obj" /S /Q
rd "Katzebase.UI\bin" /S /Q
rd "rd "Katzebase.UI\obj" /S /Q
