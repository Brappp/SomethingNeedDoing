﻿namespace SomethingNeedDoing.Framework;
/// <summary>
/// Defines the type of macro.
/// </summary>
public enum MacroType
{
    /// <summary>
    /// A native macro using the game's macro-like syntax.
    /// </summary>
    Native,

    /// <summary>
    /// A Lua script macro.
    /// </summary>
    Lua
}

/// <summary>
/// Defines the possible states of a macro.
/// </summary>
public enum MacroState
{
    /// <summary>
    /// The macro is ready to run but not yet started.
    /// </summary>
    Ready,

    /// <summary>
    /// The macro is currently running.
    /// </summary>
    Running,

    /// <summary>
    /// The macro is paused.
    /// </summary>
    Paused,

    /// <summary>
    /// The macro has completed execution.
    /// </summary>
    Completed,

    /// <summary>
    /// The macro has encountered an error.
    /// </summary>
    Error
}

/// <summary>
/// Defines the events that can trigger a macro
/// </summary>
public enum TriggerEvent
{
    None,
    OnAutoRetainerCharacterPostProcess,
    OnUpdate,
    OnCombatStart,
    OnCombatEnd,
    OnLogin,
    OnLogout,
    OnTerritoryChange,
    OnChatMessage,
    OnAddonEvent,
}
