using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ECommons.SimpleGui;
using ImGuiNET;
using SomethingNeedDoing.Exceptions;
using SomethingNeedDoing.Misc;
using System;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;

namespace SomethingNeedDoing.Interface;

/// <summary>
/// Main window for macro execution.
/// </summary>
internal class MacroWindow : ConfigWindow
{
    private readonly Regex incrementalName = new(@"(?<all> \((?<index>\d+)\))$", RegexOptions.Compiled);

    private INode? draggedNode = null;
    private MacroNode? activeMacroNode = null;
    private static TitleBarButton LockButton = null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="MacroWindow"/> class.
    /// </summary>
    public MacroWindow()
    {
        LockButton = new()
        {
            Click = OnLockButtonClick,
            Icon = Service.Configuration.LockWindow ? FontAwesomeIcon.Lock : FontAwesomeIcon.LockOpen,
            IconOffset = new(3, 2),
            ShowTooltip = () => ImGui.SetTooltip("Lock window position and size"),
        };
    }

    public static void Setup()
    {
        EzConfigGui.Window.WindowName = $"Something Need Doing {Service.Plugin.GetType().Assembly.GetName().Version}###SomethingNeedDoing";
        EzConfigGui.Window.Size = new Vector2(525, 600);
        EzConfigGui.Window.SizeCondition = ImGuiCond.FirstUseEver;
        EzConfigGui.Window.RespectCloseHotkey = false;
        EzConfigGui.Window.TitleBarButtons.Add(LockButton);
    }

    private void OnLockButtonClick(ImGuiMouseButton m)
    {
        if (m == ImGuiMouseButton.Left)
        {
            Service.Configuration.LockWindow = !Service.Configuration.LockWindow;
            LockButton.Icon = Service.Configuration.LockWindow ? FontAwesomeIcon.Lock : FontAwesomeIcon.LockOpen;
        }
    }

    private static FolderNode RootFolder => Service.Configuration.RootFolder;

    public override void Update() => EzConfigGui.Window.Flags = Service.Configuration.LockWindow ? ImGuiWindowFlags.NoMove : 0;

    /// <inheritdoc/>
    public override void PreDraw() => ImGui.PushStyleColor(ImGuiCol.ResizeGrip, 0);

    /// <inheritdoc/>
    public override void PostDraw() => ImGui.PopStyleColor();

    /// <inheritdoc/>
    public override void Draw()
    {
        ImGui.Columns(2);
        DisplayNodeTree();

        ImGui.NextColumn();
        DisplayMacroControls();
        DisplayRunningMacros();
        DisplayMacroEdit();

        ImGui.Columns(1);
    }

    private void DrawHeader()
    {
        if (ImGuiEx.IconButton(FontAwesomeIcon.Plus, "Add macro"))
        {
            var newNode = new MacroNode { Name = GetUniqueNodeName("Untitled macro") };
            RootFolder.Children.Add(newNode);
            Service.Configuration.Save();
        }

        ImGui.SameLine();
        if (ImGuiEx.IconButton(FontAwesomeIcon.FolderPlus, "Add folder"))
        {
            var newNode = new FolderNode { Name = GetUniqueNodeName("Untitled folder") };
            RootFolder.Children.Add(newNode);
            Service.Configuration.Save();
        }

        ImGui.SameLine();
        if (ImGuiEx.IconButton(FontAwesomeIcon.FileImport, "Import macro from clipboard"))
        {
            var text = Utils.ConvertClipboardToSafeString();
            var node = new MacroNode { Name = GetUniqueNodeName("Untitled macro") };
            RootFolder.Children.Add(node);

            if (Utils.IsLuaCode(text))
                node.Language = Language.Lua;

            node.Contents = text;
            Service.Configuration.Save();
        }
    }

    private void DisplayNodeTree()
    {
        DrawHeader();
        DisplayNode(RootFolder);
    }

    private void DisplayNode(INode node)
    {
        using var _ = ImRaii.PushId(node.Name);
        if (node is FolderNode folderNode)
            DisplayFolderNode(folderNode);
        else if (node is MacroNode macroNode)
            DisplayMacroNode(macroNode);
    }

    private void DisplayMacroNode(MacroNode node)
    {
        var flags = ImGuiTreeNodeFlags.Leaf;
        if (node == activeMacroNode)
        {
            flags |= ImGuiTreeNodeFlags.Selected;
        }

        ImGui.TreeNodeEx($"{node.Name}##tree", flags);

        DisplayNodePopup(node);
        NodeDragDrop(node);

        if (ImGui.IsItemClicked())
        {
            activeMacroNode = node;
        }

        ImGui.TreePop();
    }

    private void DisplayFolderNode(FolderNode node)
    {
        if (node == RootFolder)
        {
            ImGui.SetNextItemOpen(true, ImGuiCond.FirstUseEver);
        }

        var expanded = ImGui.TreeNodeEx($"{node.Name}##tree");

        DisplayNodePopup(node);
        NodeDragDrop(node);

        if (expanded)
        {
            foreach (var childNode in node.Children.ToArray())
            {
                DisplayNode(childNode);
            }

            ImGui.TreePop();
        }
    }

    private void DisplayNodePopup(INode node)
    {
        if (ImGui.BeginPopupContextItem($"##{node.Name}-popup"))
        {
            var name = node.Name;
            if (ImGui.InputText($"##rename", ref name, 100, ImGuiInputTextFlags.AutoSelectAll | ImGuiInputTextFlags.EnterReturnsTrue))
            {
                node.Name = GetUniqueNodeName(name);
                Service.Configuration.Save();
            }

            if (node is MacroNode macroNode)
            {
                if (ImGuiEx.IconButton(FontAwesomeIcon.Play, "Run"))
                {
                    RunMacro(macroNode);
                }
            }

            if (node is FolderNode folderNode)
            {
                if (ImGuiEx.IconButton(FontAwesomeIcon.Plus, "Add macro"))
                {
                    var newNode = new MacroNode { Name = GetUniqueNodeName("Untitled macro") };
                    folderNode.Children.Add(newNode);
                    Service.Configuration.Save();
                }

                ImGui.SameLine();
                if (ImGuiEx.IconButton(FontAwesomeIcon.FolderPlus, "Add folder"))
                {
                    var newNode = new FolderNode { Name = GetUniqueNodeName("Untitled folder") };
                    folderNode.Children.Add(newNode);
                    Service.Configuration.Save();
                }
            }

            if (node != RootFolder)
            {
                ImGui.SameLine();
                if (ImGuiEx.IconButton(FontAwesomeIcon.Copy, "Copy Name"))
                {
                    ImGui.SetClipboardText(node.Name);
                }

                ImGui.SameLine();
                if (ImGuiEx.IconButton(FontAwesomeIcon.TrashAlt, "Delete"))
                {
                    if (Service.Configuration.TryFindParent(node, out var parentNode))
                    {
                        parentNode!.Children.Remove(node);
                        Service.Configuration.Save();
                    }
                }

                ImGui.SameLine();
            }

            ImGui.EndPopup();
        }
    }

    private void DisplayMacroControls()
    {
        ImGui.Text("Macro Queue");

        var state = Service.MacroManager.State;

        var stateName = state switch
        {
            LoopState.NotLoggedIn => "Not Logged In",
            LoopState.Running when Service.MacroManager.PauseAtLoop => "Pausing Soon",
            LoopState.Running when Service.MacroManager.StopAtLoop => "Stopping Soon",
            _ => Enum.GetName(state),
        };

        var buttonCol = ImGuiEx.GetStyleColorVec4(ImGuiCol.Button);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, buttonCol);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, buttonCol);
        ImGui.Button($"{stateName}##LoopState", new Vector2(100, 0));
        ImGui.PopStyleColor();
        ImGui.PopStyleColor();

        ImGui.SameLine();
        if (ImGuiEx.IconButton(FontAwesomeIcon.QuestionCircle, "Help"))
            EzConfigGui.WindowSystem.Windows.FirstOrDefault(w => w.WindowName == HelpWindow.WindowName)!.IsOpen ^= true;
        ImGui.SameLine();
        if (ImGuiEx.IconButton(FontAwesomeIcon.FileExcel, "Excel Browser"))
            EzConfigGui.WindowSystem.Windows.FirstOrDefault(w => w.WindowName == ExcelWindow.WindowName)!.IsOpen ^= true;

        if (Service.MacroManager.State == LoopState.NotLoggedIn)
        { /* Nothing to do */
        }
        else if (Service.MacroManager.State == LoopState.Stopped)
        { /* Nothing to do */
        }
        else if (Service.MacroManager.State == LoopState.Waiting)
        { /* Nothing to do */
        }
        else if (Service.MacroManager.State == LoopState.Paused)
        {
            ImGui.SameLine();
            if (ImGuiEx.IconButton(FontAwesomeIcon.Play, "Resume"))
                Service.MacroManager.Resume();

            ImGui.SameLine();
            if (ImGuiEx.IconButton(FontAwesomeIcon.StepForward, "Step"))
                Service.MacroManager.NextStep();

            ImGui.SameLine();
            if (ImGuiEx.IconButton(FontAwesomeIcon.TrashAlt, "Clear"))
                Service.MacroManager.Stop();
        }
        else if (Service.MacroManager.State == LoopState.Running)
        {
            ImGui.SameLine();
            if (ImGuiEx.IconButton(FontAwesomeIcon.Pause, "Pause (hold control to pause at next /loop)"))
            {
                var io = ImGui.GetIO();
                var ctrlHeld = io.KeyCtrl;

                Service.MacroManager.Pause(ctrlHeld);
            }

            ImGui.SameLine();
            if (ImGuiEx.IconButton(FontAwesomeIcon.Stop, "Stop (hold control to stop at next /loop)"))
            {
                var io = ImGui.GetIO();
                var ctrlHeld = io.KeyCtrl;

                Service.MacroManager.Stop(ctrlHeld);
            }
        }
    }

    private void DisplayRunningMacros()
    {
        ImGui.PushItemWidth(-1);

        var style = ImGui.GetStyle();
        var runningHeight = (ImGui.CalcTextSize("CalcTextSize").Y * ImGuiHelpers.GlobalScale * 3) + (style.FramePadding.Y * 2) + (style.ItemSpacing.Y * 2);
        if (ImGui.BeginListBox("##running-macros", new Vector2(-1, runningHeight)))
        {
            var macroStatus = Service.MacroManager.MacroStatus;
            for (var i = 0; i < macroStatus.Length; i++)
            {
                var (name, stepIndex) = macroStatus[i];
                var text = name;
                if (i == 0 || stepIndex > 1)
                    text += $" (step {stepIndex})";
                ImGui.Selectable($"{text}##{Guid.NewGuid()}", i == 0);
            }

            ImGui.EndListBox();
        }

        var contentHeight = (ImGui.CalcTextSize("CalcTextSize").Y * ImGuiHelpers.GlobalScale * 5) + (style.FramePadding.Y * 2) + (style.ItemSpacing.Y * 4);
        var macroContent = Service.MacroManager.CurrentMacroContent();
        if (ImGui.BeginListBox("##current-macro", new Vector2(-1, contentHeight)))
        {
            var stepIndex = Service.MacroManager.CurrentMacroStep();
            if (stepIndex == -1)
            {
                ImGui.Selectable("Looping", true);
            }
            else
            {
                for (var i = stepIndex; i < macroContent.Length; i++)
                {
                    var step = macroContent[i];
                    var isCurrentStep = i == stepIndex;
                    ImGui.Selectable(step, isCurrentStep);
                }
            }

            ImGui.EndListBox();
        }

        ImGui.PopItemWidth();
    }

    private void DisplayMacroEdit()
    {
        var node = activeMacroNode;
        if (node is null)
            return;

        ImGui.Text("Macro Editor");

        if (ImGuiEx.IconButton(FontAwesomeIcon.Play, "Run"))
            RunMacro(node);

        ImGui.SameLine();
        if (ImGuiEx.IconButton(FontAwesomeIcon.TimesCircle, "Close"))
        {
            activeMacroNode = null;
        }

        var luaEnabled = node.Language == Language.Lua;
        if (luaEnabled)
        {
            ImGui.PushStyleColor(ImGuiCol.Button, ImGuiColors.HealerGreen);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ImGuiColors.HealerGreen);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, ImGuiColors.ParsedGreen);
        }

        ImGui.SameLine();
        if (ImGuiEx.IconButton(FontAwesomeIcon.Code, "Lua script"))
        {
            if (node.Language == Language.Lua)
                node.Language = Language.Native;
            else
                node.Language = Language.Lua;
            Service.Configuration.Save();
        }

        if (luaEnabled)
            ImGui.PopStyleColor(3);

        if (!luaEnabled)
        {
            var sb = new StringBuilder("Toggle CraftLoop");
            var craftLoopEnabled = node.CraftingLoop;

            if (craftLoopEnabled)
            {
                ImGui.PushStyleColor(ImGuiCol.Button, ImGuiColors.HealerGreen);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ImGuiColors.HealerGreen);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, ImGuiColors.ParsedGreen);

                sb.AppendLine(" (0=disabled, -1=infinite)");
                sb.AppendLine($"When enabled, your macro is modified as follows:");
                sb.AppendLine(
                    ActiveMacro.ModifyMacroForCraftLoop("[YourMacro]", true, node.CraftLoopCount)
                    .Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None)
                    .Select(line => $"- {line}")
                    .Aggregate(string.Empty, (s1, s2) => $"{s1}\n{s2}"));
            }

            ImGui.SameLine();
            if (ImGuiEx.IconButton(FontAwesomeIcon.Sync, sb.ToString()))
            {
                node.CraftingLoop ^= true;
                Service.Configuration.Save();
            }

            if (craftLoopEnabled)
                ImGui.PopStyleColor(3);

            if (node.CraftingLoop)
            {
                ImGui.SameLine();
                ImGui.PushItemWidth(50);

                var v_min = -1;
                var v_max = 999;
                var loops = node.CraftLoopCount;
                if (ImGui.InputInt("##CraftLoopCount", ref loops, 0) || MouseWheelInput(ref loops))
                {
                    if (loops < v_min)
                        loops = v_min;

                    if (loops > v_max)
                        loops = v_max;

                    node.CraftLoopCount = loops;
                    Service.Configuration.Save();
                }

                ImGui.PopItemWidth();
            }
        }

        ImGui.SameLine();
        var buttonSize = ImGuiHelpers.GetButtonSize(FontAwesomeIcon.FileImport.ToIconString());
        ImGui.SetCursorPosX(ImGui.GetContentRegionMax().X - buttonSize.X - ImGui.GetStyle().WindowPadding.X);
        if (ImGuiEx.IconButton(FontAwesomeIcon.FileImport, "Import from clipboard"))
        {
            var text = Utils.ConvertClipboardToSafeString();

            if (Utils.IsLuaCode(text))
                node.Language = Language.Lua;

            node.Contents = text;
            Service.Configuration.Save();
        }

        ImGui.PushItemWidth(-1);
        var useMono = !Service.Configuration.DisableMonospaced;
        using var font = ImRaii.PushFont(UiBuilder.MonoFont, useMono);

        var contents = node.Contents;
        if (ImGui.InputTextMultiline($"##{node.Name}-editor", ref contents, 100_000, new Vector2(-1, -1)))
        {
            node.Contents = contents;
            Service.Configuration.Save();
        }

        ImGui.PopItemWidth();
    }

    private string GetUniqueNodeName(string name)
    {
        var nodeNames = Service.Configuration.GetAllNodes()
            .Select(node => node.Name)
            .ToList();

        while (nodeNames.Contains(name))
        {
            var match = incrementalName.Match(name);
            if (match.Success)
            {
                var all = match.Groups["all"].Value;
                var index = int.Parse(match.Groups["index"].Value) + 1;
                name = name[..^all.Length];
                name = $"{name} ({index})";
            }
            else
            {
                name = $"{name} (1)";
            }
        }

        return name.Trim();
    }

    private void NodeDragDrop(INode node)
    {
        if (node != RootFolder)
        {
            if (ImGui.BeginDragDropSource())
            {
                draggedNode = node;
                ImGui.Text(node.Name);
                ImGui.SetDragDropPayload("NodePayload", IntPtr.Zero, 0);
                ImGui.EndDragDropSource();
            }
        }

        if (ImGui.BeginDragDropTarget())
        {
            var payload = ImGui.AcceptDragDropPayload("NodePayload");

            bool nullPtr;
            unsafe
            {
                nullPtr = payload.NativePtr == null;
            }

            var targetNode = node;
            if (!nullPtr && payload.IsDelivery() && draggedNode != null)
            {
                if (!Service.Configuration.TryFindParent(draggedNode, out var draggedNodeParent))
                    throw new Exception($"Could not find parent of node \"{draggedNode.Name}\"");

                if (targetNode is FolderNode targetFolderNode)
                {
                    draggedNodeParent!.Children.Remove(draggedNode);
                    targetFolderNode.Children.Add(draggedNode);
                    Service.Configuration.Save();
                }
                else
                {
                    if (!Service.Configuration.TryFindParent(targetNode, out var targetNodeParent))
                        throw new Exception($"Could not find parent of node \"{targetNode.Name}\"");

                    var targetNodeIndex = targetNodeParent!.Children.IndexOf(targetNode);
                    if (targetNodeParent == draggedNodeParent)
                    {
                        var draggedNodeIndex = targetNodeParent.Children.IndexOf(draggedNode);
                        if (draggedNodeIndex < targetNodeIndex)
                        {
                            targetNodeIndex -= 1;
                        }
                    }

                    draggedNodeParent!.Children.Remove(draggedNode);
                    targetNodeParent.Children.Insert(targetNodeIndex, draggedNode);
                    Service.Configuration.Save();
                }

                draggedNode = null;
            }

            ImGui.EndDragDropTarget();
        }
    }

    private void RunMacro(MacroNode node)
    {
        try
        {
            Service.MacroManager.EnqueueMacro(node);
        }
        catch (MacroSyntaxError ex)
        {
            Service.ChatManager.PrintError($"{ex.Message}");
        }
        catch (Exception ex)
        {
            Service.ChatManager.PrintError($"Unexpected error");
            Svc.Log.Error(ex, "Unexpected error");
        }
    }

    private bool MouseWheelInput(ref int iv)
    {
        if (ImGui.IsItemHovered())
        {
            var mouseDelta = (int)ImGui.GetIO().MouseWheel;  // -1, 0, 1
            if (mouseDelta != 0)
            {
                iv += mouseDelta;
                return true;
            }
        }

        return false;
    }
}
