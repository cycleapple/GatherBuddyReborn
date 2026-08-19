using System.Linq;
using ECommons.GameHelpers;
using GatherBuddy.AutoGather.Movement;
using GatherBuddy.Plugin;
using GatherBuddy.SeFunctions;

namespace GatherBuddy.AutoGather;

public partial class AutoGather
{
    private AdvancedUnstuckCheckResult CheckAntiStuck(bool isPathGenerating, bool isPathing)
    {
        _antiStuckManager.SetContext(CurrentDestination, IsGathering);
        var result = _antiStuckManager.Tick(isPathGenerating, isPathing);

        if (_antiStuckManager.ShouldExecuteDrasticAction() && CanAct && !TaskManager.IsBusy)
            ExecuteAntiStuckDrasticAction();

        return result;
    }

    private void ExecuteAntiStuckDrasticAction()
    {
        StopNavigation();
        switch (_antiStuckManager.GetDrasticAction())
        {
            case AutoGatherConfig.PositionUnstuckAction.TeleportAetheryte:
            {
                if (!GatherBuddy.GameData.Territories.TryGetValue(Dalamud.ClientState.TerritoryType, out var territory))
                    return;

                var aetheryte = territory.Aetherytes.FirstOrDefault(a => Teleporter.IsAttuned(a.Id));
                if (aetheryte == null)
                {
                    GatherBuddy.Log.Warning("AntiStuck: 此區域找不到已解鎖的以太之光。");
                    return;
                }

                GatherBuddy.Log.Warning($"AntiStuck: 傳送至 {aetheryte.Name} 後繼續自動採集。");
                Teleporter.Teleport(aetheryte.Id);
                _antiStuckManager.MarkDrasticActionExecuted();
                break;
            }
            case AutoGatherConfig.PositionUnstuckAction.GoHome:
            {
                if (!Lifestream.Enabled || Lifestream.IsBusy())
                {
                    GatherBuddy.Log.Warning("AntiStuck: Lifestream 未就緒，無法執行回家措施。");
                    return;
                }

                var command = GatherBuddy.Config.AutoGatherConfig.LifestreamCommand.Trim();
                if (command.StartsWith("/li "))
                    command = command[4..];
                GatherBuddy.Log.Warning("AntiStuck: 執行回家並停止自動採集。");
                Lifestream.ExecuteCommand(command);
                _antiStuckManager.MarkDrasticActionExecuted();
                Enabled = false;
                break;
            }
        }
    }
}
