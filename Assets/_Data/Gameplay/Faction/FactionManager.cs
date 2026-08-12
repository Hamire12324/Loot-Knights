public static class FactionManager
{
    public static bool CanAttack(Faction attacker, Faction target)
    {
        if (attacker == Faction.Hero && (target == Faction.Enemy || target == Faction.Boss))
            return true;

        if ((attacker == Faction.Enemy || attacker == Faction.Boss) &&
            (target == Faction.Hero || target == Faction.Ally))
            return true;

        if (attacker == Faction.Ally && target == Faction.Enemy)
            return true;

        return false;
    }
}
