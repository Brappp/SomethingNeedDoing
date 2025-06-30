﻿using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using DalamudCodeEditor;
using TextEditor = DalamudCodeEditor.TextEditor.Editor;
using SomethingNeedDoing.Core.Interfaces;

namespace SomethingNeedDoing.Gui.Editor;

/// <summary>
/// DalamudCodeEditor TextEditor wrapper.
/// </summary>
public class CodeEditor : IDisposable
{
    private readonly LuaLanguageDefinition _lua;
    private readonly TextEditor _editor = new();

    private readonly Dictionary<MacroType, LanguageDefinition> _languages;

    private IMacro? macro = null;

    public CodeEditor(LuaLanguageDefinition lua)
    {
        _lua = lua;
        _languages = new() { { MacroType.Lua, _lua }, { MacroType.Native, new NativeMacroLanguageDefinition() } };
        Config.ConfigFileChanged += RefreshContent;
    }

    public int Lines => _editor.Buffer.LineCount;
    public bool ReadOnly
    {
        get => _editor.IsReadOnly;
        set => _editor.SetReadOnly(value);
    }

    public bool IsHighlightingSyntax
    {
        get => _editor.Colorizer.Enabled;
        set => _editor.Colorizer.SetEnabled(value);
    }

    public bool IsShowingWhitespace
    {
        get => _editor.Style.ShowWhitespace;
        set => _editor.Style.SetShowWhitespace(value);
    }

    public bool IsShowingLineNumbers
    {
        get => _editor.Style.ShowLineNumbers;
        set => _editor.Style.SetShowLineNumbers(value);
    }

    public int Column => _editor.Cursor.GetPosition().Column;

    public void SetMacro(IMacro macro)
    {
        if (this.macro?.Id == macro.Id)
            return;

        this.macro = macro;
        _editor.Buffer.SetText(macro.Content);
        _editor.UndoManager.Clear();

        if (_languages.TryGetValue(macro.Type, out var language))
            _editor.Language = language;
    }

    public void RefreshContent()
    {
        if (macro != null)
            _editor.Buffer.SetText(macro.Content);
    }

    public string GetContent() => _editor.Buffer.GetText();

    public bool Draw()
    {
        if (macro == null)
            return false;

        using var font = ImRaii.PushFont(UiBuilder.MonoFont);
        _editor.Draw(macro.Name);
        return _editor.Buffer.IsDirty;
    }

    public void Dispose() => Config.ConfigFileChanged -= RefreshContent;
}
