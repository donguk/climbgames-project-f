using System.Collections.Generic;

namespace ClimbGames.Project_HC
{
    public class FishingPlayerManager : Singleton<FishingPlayerManager>
    {
        private Dictionary<int, FishingPlayer> players = new Dictionary<int, FishingPlayer>();

        public FishingPlayer LocalPlayer => GetOrCreatePlayer(0);

        public FishingPlayer GetOrCreatePlayer(int id)
        {
            if (players.TryGetValue(id, out var player) == false)
                players[id] = player = new FishingPlayer(id);

            return player;
        }
    }
}