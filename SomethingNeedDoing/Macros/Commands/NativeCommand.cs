using SomethingNeedDoing.Macros.Commands.Modifiers;
using SomethingNeedDoing.Misc;
using System.Threading;
using System.Threading.Tasks;

namespace SomethingNeedDoing.Grammar.Commands;

internal class NativeCommand : MacroCommand
{
    private NativeCommand(string text, WaitModifier wait) : base(text, wait) { }

    public static NativeCommand Parse(string text)
    {
        _ = WaitModifier.TryParse(ref text, out var waitModifier);

        return new NativeCommand(text, waitModifier);
    }

    public override async Task Execute(ActiveMacro macro, CancellationToken token)
    {
        Svc.Log.Debug($"Executing: {Text}");

        Service.ChatManager.SendMessage($"{(new[] { "/", "<" }.Any(Text.StartsWith) ? Text : $"/e {Text}")}");

        await PerformWait(token);
    }
}
