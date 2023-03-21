attrib ".vs" -h -a -r -s
attrib *.* -h -a -r -s

rd ".vs" /S /Q
rd "packages" /S /Q

rd "Katzebase.Engine\bin" /S /Q
rd "Katzebase.Engine\obj" /S /Q
rd "Katzebase.Library\bin" /S /Q
rd "Katzebase.Library\obj" /S /Q
rd "Katzebase.Models\bin" /S /Q
rd "Katzebase.Models\obj" /S /Q
rd "Katzebase.Service\bin" /S /Q
rd "Katzebase.Service\obj" /S /Q
rd "Katzebase.TestHarness\bin" /S /Q
rd "Katzebase.TestHarness\obj" /S /Q
rd "Katzebase.CodeGeneration\bin" /S /Q
rd "Katzebase.CodeGeneration\obj" /S /Q
