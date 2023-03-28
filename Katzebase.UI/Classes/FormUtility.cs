using Shell32;

namespace Katzebase.UI.Classes
{
    internal static class FormUtility
    {
        public static Shell shell = new();
        public static Folder RecyclingBin = shell.NameSpace(10);

        /// <summary>
        /// Delete a file or folder to the recyclebin.
        /// </summary>
        /// <param name="filePath"></param>
        public static void Recycle(string Path)
        {
            RecyclingBin.MoveHere(Path);
        }

        public static TreeNode MacroNode(string text, string value, string tip)
        {
            var node = new TreeNode(text);
            node.Tag = value;
            node.ToolTipText = tip;
            return node;
        }

        public static TreeNode? FindNode(TreeNode root, string text)
        {
            foreach (var node in root.Nodes.Cast<TreeNode>())
            {
                if (node.Text == text)
                {
                    return node;
                }
            }
            return null;
        }

        public static TreeNode[] GetMacroTreeNoded()
        {
            var nodes = new List<TreeNode>();
            var configNode = new TreeNode("Configuration");

            var connectivityNode = new TreeNode("Connectivity");
            connectivityNode.Nodes.Add(MacroNode("$Application", "$Application: Workload Generator", "<string> The name of the application that will be reported to SQL server."));
            connectivityNode.Nodes.Add(MacroNode("$Database", "$Database: tempdb", "<string> The name of the database to set the database context."));
            connectivityNode.Nodes.Add(MacroNode("$IntegratedSecurity", "$IntegratedSecurity True", "<boolean> True or False depending on whether integrated security will be used as opposed to SQL authentication."));
            connectivityNode.Nodes.Add(MacroNode("$Password", "$Password: ", "<string> The user password to use for the login to SQL server. If IntegratedSecurity is set to false."));
            connectivityNode.Nodes.Add(MacroNode("$Server", "$Server: localhost", "<string> The name of the server to connect to."));
            connectivityNode.Nodes.Add(MacroNode("$UserName", "$UserName: ", "<string> The user name to use for the login to SQL server. If IntegratedSecurity is set to false."));
            configNode.Nodes.Add(connectivityNode);
            connectivityNode.Expand();

            var generalNode = new TreeNode("General");
            generalNode.Nodes.Add(MacroNode("$CommandTimeout", "$CommandTimeout: 30", "<integer> The number of seconds for each command timeout."));
            generalNode.Nodes.Add(MacroNode("$Mode", "$Mode Query", "<Query|NonQuery> [Query] will execute the statement and then retrieve each row, [NonQuery] will simply execute the SQL statement."));
            configNode.Nodes.Add(generalNode);
            generalNode.Expand();

            var throttleNode = new TreeNode("Throttle");
            throttleNode.Nodes.Add(MacroNode("$CommandDelay", "$CommandDelay: 10:100", "<integer> The number of milliseconds to sleep between SQL statements, per thread."));
            throttleNode.Nodes.Add(MacroNode("$RampUpDelay", "$RampUpDelay: 1000:5000", "<integer> The number of milliseconds to sleep between each thread creation when starting up."));
            throttleNode.Nodes.Add(MacroNode("$RecordDelay", "$RecordDelay: 1:5", "<integer> The number of milliseconds to sleep between each row retrieval, per thread."));
            throttleNode.Nodes.Add(MacroNode("$Threads", "$Threads: 2", "<integer> The number of threads for this file."));
            configNode.Nodes.Add(throttleNode);
            throttleNode.Expand();

            var timeboxingNode = new TreeNode("Timeboxing");
            timeboxingNode.Nodes.Add(MacroNode("$EndTime", "$EndTime: 8PM", "<string> The end time of the day that the script will be run."));
            timeboxingNode.Nodes.Add(MacroNode("$PeakRunDays", "$PeakRunDays: MON,TUE,WED,THU,FRI", "<string> The days of the week that will be considered for peak-time workload multiplication."));
            timeboxingNode.Nodes.Add(MacroNode("$PeakTimeBegin", "$PeakTimeBegin: 11AM", "<string> The time of the day at which the standard load will begin to be increased by the $PeakTimeMultiplier."));
            timeboxingNode.Nodes.Add(MacroNode("$PeakTimeEnd", "$PeakTimeEnd: 2PM", "<string> The time of the day at which the $PeakTimeMultiplier will be completely disregarded."));
            timeboxingNode.Nodes.Add(MacroNode("$PeakTimeMultiplier", "$PeakTimeMultiplier: 10", "<integer 1-100> The percentage of $CommandDelay that will be subtracted as peak time approaches. If a $PeakTimeBegin/$PeakTimeEnd time are specified, the default is 10."));
            timeboxingNode.Nodes.Add(MacroNode("$RunDays", "$RunDays: SUN,MON,TUE,WED,THU,FRI,SAT", "<string> The days of the week that the workload will be run."));
            timeboxingNode.Nodes.Add(MacroNode("$StartTime", "$StartTime: 6AM", "<string> The start time of the day that the script will be run."));
            configNode.Nodes.Add(timeboxingNode);
            timeboxingNode.Expand();

            var macrosNode = new TreeNode("Functions");

            macrosNode.Nodes.Add(MacroNode("Write(<text>)", "::Write(\"text\")", "Writes text to the output tab, usful for debugging."));

            macrosNode.Nodes.Add(MacroNode("Left(<text>, <length>)", "::Left(\"text\", 1)", "Returns the given left number of characters from the given text."));
            macrosNode.Nodes.Add(MacroNode("Length(<text>)", "::Length(\"text\")", "Returns the length of the given text."));
            macrosNode.Nodes.Add(MacroNode("PadLeft(<text>, <totalWidth>, <paddingCharacter>", "::PadLeft(\"text\", 10)", "Adds a specified number of characters to the left side of the given text."));
            macrosNode.Nodes.Add(MacroNode("PadRight(<text>, <totalWidth, <paddingCharacter>", "::PadRight(\"text\", 10)", "Adds a specified number of characters to the right side of the given text."));
            macrosNode.Nodes.Add(MacroNode("Right(<text>, <length>)", "::Right(\"text\", 1)", "Returns the given right number of characters from the given text."));
            macrosNode.Nodes.Add(MacroNode("SubString(<text>, <startIndex>, <length>)", "::SubString(\"text\", 1, 2)", "Returns a sub string starting at a position of a given length."));
            macrosNode.Nodes.Add(MacroNode("TitleCase(<text>)", "::TitleCase(\"text\")", "Converts the given text to title case."));
            macrosNode.Nodes.Add(MacroNode("ToLower(<text>)", "::ToLower(\"text\")", "Converts the given text to lower case."));
            macrosNode.Nodes.Add(MacroNode("ToUpper(<text>)", "::ToUpper(\"text\")", "Converts the given text to upper case."));

            macrosNode.Nodes.Add(MacroNode("Time(<format>)", "::Time()", "Returns a time (optionally also a date depending on the format)."));
            macrosNode.Nodes.Add(MacroNode("Date<format>)", "::Date()", "Returns a date (optionally also a time depending on the format)."));

            macrosNode.Nodes.Add(MacroNode("Double(<min>, <max>)", "::Double(0.0, 100.0)", "Returns a random decimal number between a given min and max."));
            macrosNode.Nodes.Add(MacroNode("Int(<min>, <max>)", "::Int(0, 100)", "Returns a random integer between a given min and max."));

            macrosNode.Nodes.Add(MacroNode("DataSample(<assetName>)", "::DS(\"assetName\")", "Gets a random line from the specified asset file."));
            macrosNode.Nodes.Add(MacroNode("Get(<variableName>)", "::Get(\"variabkeName\")", "Gets the value of a given variable."));
            macrosNode.Nodes.Add(MacroNode("GUID()", "::GUID", "Generates and returns a random GUID."));
            macrosNode.Nodes.Add(MacroNode("RandomString(<length>, <allowedCharacters>", "::RandomString(10)", "Geenrates a randon string of the specified length."));
            macrosNode.Nodes.Add(MacroNode("Set(<variableName>)", "::Set(\"variableName\", \"someValue\")", "Sets a variable for later use in a script."));

            macrosNode.Nodes.Add(MacroNode("SHA256(<text>)", "::SHA256(\"text\")", "Returns the SHA256 hash of the given text."));
            macrosNode.Nodes.Add(MacroNode("SHA1(<text>)", "::SHA1(\"text\")", "Returns the SHA1 hash of the given text."));

            macrosNode.Nodes.Add(MacroNode("URLDecode(<text>)", "URLDecode(\"text\")", "Returns the URL decoded version of the given text."));
            macrosNode.Nodes.Add(MacroNode("URLEncode(<text>)", "::URLEncode(\"text\")", "Returns the URL encoded version of the given text."));

            macrosNode.Nodes.Add(MacroNode("MachineName()", "::MachineName", "Returns the name of the current computer."));
            macrosNode.Nodes.Add(MacroNode("Sleep(<milliseconds>)", "::Sleep(1)", "Causes the thread to wait for the specified number of milliseconds."));
            macrosNode.Nodes.Add(MacroNode("SQL(<statement>)", "::SQL(\"Select TOP 1...\")", "Executes a scalar SQL statement and substitues its value for the macro."));
            macrosNode.Nodes.Add(MacroNode("TickCount()", "::TickCount", "Returns the number of milliseconds since the computer was started."));
            macrosNode.Nodes.Add(MacroNode("UserName()", "::UserName", "Returns the user name of the executing user."));

            var assetsNode = new TreeNode("Assets");
            nodes.Add(assetsNode);

            nodes.Add(configNode);
            configNode.Expand();

            nodes.Add(macrosNode);
            macrosNode.Expand();

            return nodes.ToArray();
        }

        public static int SortChildNodes(TreeNode node)
        {
            int moves = 0;

            for (int i = 0; i < node.Nodes.Count - 1; i++)
            {
                if (node.Nodes[i].Text.CompareTo(node.Nodes[i + 1].Text) > 0)
                {
                    int nodeIndex = node.Nodes[i].Index;
                    var nodeCopy = node.Nodes[i].Clone() as TreeNode;
                    node.Nodes.Remove(node.Nodes[i]);

                    node.Nodes.Insert(nodeIndex + 1, nodeCopy);
                    moves++;
                }
                else if (node.Nodes[i + 1].Text.CompareTo(node.Nodes[i].Text) < 0)
                {
                    int nodeIndex = node.Nodes[i].Index;
                    var nodeCopy = node.Nodes[i].Clone() as TreeNode;
                    node.Nodes.Remove(node.Nodes[i]);

                    node.Nodes.Insert(nodeIndex - 1, nodeCopy);
                    moves++;
                }
            }

            if (moves > 0)
            {
                return SortChildNodes(node);
            }

            return moves;
        }

        public static string TrimOneTabStop(string text)
        {
            int index = 0;

            if (text[index] == '\t')
            {
                index++;
            }
            else
            {
                while (index < 4)
                {
                    if (text[index] != ' ')
                    {
                        break;
                    }
                    index++;
                }
            }

            return text.Substring(index);
        }

        public static Image TransparentImage(Image image)
        {
            Bitmap toolBitmap = new(image);
            toolBitmap.MakeTransparent(Color.Magenta);
            return toolBitmap;
        }

        public static Image ImageFromBytes(byte[] imageBytes)
        {
            using (var ms = new MemoryStream(imageBytes))
            {
                return Image.FromStream(ms);
            }
        }

        #region Tree node factories.

        public static ProjectTreeNode CreateProjectNode(string projectPath)
        {
            var name = Path.GetFileName(projectPath.TrimEnd(Path.DirectorySeparatorChar));
            var node = new ProjectTreeNode(name)
            {
                NodeType = Constants.ProjectNodeType.Project,
                FullFilePath = projectPath,
                ImageKey = "Project",
                SelectedImageKey = "Project"
            };

            return node;
        }

        public static ProjectTreeNode CreateFolderNode(string filePath)
        {
            var name = Path.GetFileName(filePath.TrimEnd(Path.DirectorySeparatorChar));
            var node = new ProjectTreeNode(name)
            {
                NodeType = Constants.ProjectNodeType.Folder,
                FullFilePath = filePath,
                ImageKey = "Folder",
                SelectedImageKey = "Folder"
            };

            return node;
        }

        public static ProjectTreeNode CreateScriptNode(string filePath)
        {
            var name = Path.GetFileName(filePath.TrimEnd(Path.DirectorySeparatorChar));
            var node = new ProjectTreeNode(name)
            {
                NodeType = Constants.ProjectNodeType.Script,
                FullFilePath = filePath,
                ImageKey = "Script",
                SelectedImageKey = "Script",
            };

            return node;
        }

        public static ProjectTreeNode CreateAssetsNode(string filePath)
        {
            var name = Path.GetFileName(filePath.TrimEnd(Path.DirectorySeparatorChar));
            var node = new ProjectTreeNode(name)
            {
                NodeType = Constants.ProjectNodeType.Assets,
                FullFilePath = filePath,
                ImageKey = "Assets",
                SelectedImageKey = "Assets",
            };

            return node;
        }

        public static ProjectTreeNode CreateAssetNode(string filePath)
        {
            var name = Path.GetFileName(filePath.TrimEnd(Path.DirectorySeparatorChar));
            var node = new ProjectTreeNode(name)
            {
                NodeType = Constants.ProjectNodeType.Asset,
                FullFilePath = filePath,
                ImageKey = "Asset",
                SelectedImageKey = "Asset",
            };

            return node;
        }
        public static ProjectTreeNode CreateNoteNode(string filePath)
        {
            var name = Path.GetFileName(filePath.TrimEnd(Path.DirectorySeparatorChar));
            var node = new ProjectTreeNode(name)
            {
                NodeType = Constants.ProjectNodeType.Note,
                FullFilePath = filePath,
                ImageKey = "Note",
                SelectedImageKey = "Note",
            };

            return node;
        }

        public static ProjectTreeNode CreateWorkloadsNode(string filePath)
        {
            var name = Path.GetFileName(filePath.TrimEnd(Path.DirectorySeparatorChar));
            var node = new ProjectTreeNode(name)
            {
                NodeType = Constants.ProjectNodeType.Workloads,
                FullFilePath = filePath,
                ImageKey = "Workloads",
                SelectedImageKey = "Workloads",
            };

            return node;
        }

        public static ProjectTreeNode CreateWorkloadNode(string filePath)
        {
            var name = Path.GetFileName(filePath.TrimEnd(Path.DirectorySeparatorChar));
            var node = new ProjectTreeNode(name)
            {
                NodeType = Constants.ProjectNodeType.Workload,
                FullFilePath = filePath,
                ImageKey = "Workload",
                SelectedImageKey = "Workload",
            };

            return node;
        }

        #endregion
    }
}
