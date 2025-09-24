using System;
using System.Collections.Generic;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;

namespace CS2DeleteWeapons;

public class DeleteWeaponsPlugin : BasePlugin
{
    private const float CleanupDelaySeconds = 0.25f;

    private static readonly HashSet<string> ExcludedWeapons = new(StringComparer.OrdinalIgnoreCase)
    {
        "weapon_c4" // keep the bomb intact to avoid breaking objectives
    };

    private bool _globalCleanupQueued;

    public override string ModuleName => "cs2-deleteweapons";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "Micka";
    public override string ModuleDescription => "Supprime les armes au sol après le début d'un round et au spawn des joueurs.";

    public override void Load(bool hotReload)
    {
        RegisterEventHandler<EventRoundStart>(OnRoundStart);
        RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);

        RegisterListener<Listeners.OnEntitySpawned>(OnEntitySpawned);
    }

    private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        ScheduleCleanup();
        return HookResult.Continue;
    }

    private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        ScheduleCleanup();
        return HookResult.Continue;
    }

    private void OnEntitySpawned(CEntityInstance entity)
    {
        if (entity is not CBasePlayerWeapon weapon)
        {
            return;
        }

        if (!ShouldManageWeapon(weapon))
        {
            return;
        }

        AddTimer(CleanupDelaySeconds, () => RemoveWeaponIfGrounded(weapon));
    }

    private void ScheduleCleanup()
    {
        if (_globalCleanupQueued)
        {
            return;
        }

        _globalCleanupQueued = true;
        AddTimer(CleanupDelaySeconds, () =>
        {
            _globalCleanupQueued = false;
            RemoveAllGroundWeapons();
        });
    }

    private void RemoveAllGroundWeapons()
    {
        foreach (var weapon in Utilities.FindAllEntitiesByDesignerName<CBasePlayerWeapon>("weapon_"))
        {
            RemoveWeaponIfGrounded(weapon);
        }
    }

    private void RemoveWeaponIfGrounded(CBasePlayerWeapon weapon)
    {
        if (!ShouldManageWeapon(weapon))
        {
            return;
        }

        if (!weapon.IsValid)
        {
            return;
        }

        var ownerHandle = weapon.OwnerEntity;
        if (ownerHandle.IsValid && ownerHandle.Value != null && ownerHandle.Value.IsValid)
        {
            return;
        }

        weapon.Remove();
    }

    private static bool ShouldManageWeapon(CBasePlayerWeapon weapon)
    {
        if (!weapon.IsValid)
        {
            return false;
        }

        var designerName = weapon.DesignerName;
        if (string.IsNullOrEmpty(designerName))
        {
            return false;
        }

        if (!designerName.StartsWith("weapon_", StringComparison.Ordinal))
        {
            return false;
        }

        return !ExcludedWeapons.Contains(designerName);
    }
}
