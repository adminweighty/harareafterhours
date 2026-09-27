using System;
using UnityEngine;

namespace HarareAfterHours
{
    [Serializable]
    public sealed class PlayerGuidance
    {
        public int version = 1;
        public string objective;
        public string instruction;
        public string availability = "Playable campaign: M01–M30. Progress, checkpoints and choices save between sessions.";

        public static PlayerGuidance For(MissionDirector mission)
        {
            if (mission == null) return new PlayerGuidance { objective = "Connecting to the city", instruction = "Your next step will appear when the city is ready." };
            if (mission.ActiveMissionLevel > 1)
            {
                return new PlayerGuidance
                {
                    objective = mission.ObjectiveText,
                    instruction = mission.ExtendedMissionComplete
                        ? "This mission is saved. The next available mission will start automatically."
                        : mission.ControlHint + " Follow the gold objective marker.",
                };
            }
            string next = mission.State switch
            {
                MissionState.MeetRudo => "Follow the gold marker to Rudo. Stand beside her and tap TALK to accept the delivery.",
                MissionState.ChaseRunner => "Follow the marked satchel runner on foot, or take Tino's hatchback through the service route. Get close and tap RECOVER; you do not need to shoot the runner.",
                MissionState.YardRecovery => "Go to the marked service yard. Approach the satchel and tap RECOVER, or approach Tino and tap COMMAND to block the exit.",
                MissionState.ReturnToChipo => "Follow the gold marker to Chipo. Stand beside her and tap RETURN to finish the delivery.",
                _ => "M01 is complete. M02 will start automatically. Open Menu if the next objective has not appeared yet.",
            };
            var room = VenueInterior.Active;
            return new PlayerGuidance
            {
                objective = room != null ? room.Objective : mission.ObjectiveText,
                instruction = room != null ? room.GuideText + "\n\nOutside: " + next : next,
            };
        }
    }
}
