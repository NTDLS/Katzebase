attrib ".vs" -h -a -r -s
attrib *.* -h -a -r -s

rd ".vs" /S /Q
rd "packages" /S /Q

rd "Dokdex.Engine\bin" /S /Q
rd "Dokdex.Engine\obj" /S /Q
rd "Dokdex.Library\bin" /S /Q
rd "Dokdex.Library\obj" /S /Q
rd "Dokdex.Models\bin" /S /Q
rd "Dokdex.Models\obj" /S /Q
rd "Dokdex.Service\bin" /S /Q
rd "Dokdex.Service\obj" /S /Q
rd "Dokdex.TestHarness\bin" /S /Q
rd "Dokdex.TestHarness\obj" /S /Q
rd "Dokdex.CodeGeneration\bin" /S /Q
rd "Dokdex.CodeGeneration\obj" /S /Q
