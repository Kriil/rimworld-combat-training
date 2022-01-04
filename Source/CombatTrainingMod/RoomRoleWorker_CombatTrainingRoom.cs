using Verse;

namespace KriilMod_CD
{
    public class RoomRoleWorker_CombatTrainingRoom : RoomRoleWorker
    {
        public override float GetScore(Room room)
        {
            var num = 0;
            var containedAndAdjacentThings = room.ContainedAndAdjacentThings;
            foreach (var thing in containedAndAdjacentThings)
            {
                if (thing is Building_CombatDummy)
                {
                    num++;
                }
            }

            return num * 5f;
        }
    }
}