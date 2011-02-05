using WCell.Constants.NPCs;
using WCell.RealmServer.Factions;
using WCell.RealmServer.NPCs;
using WCell.RealmServer.NPCs.Spawns;
using WCell.Util.Variables;

namespace WCell.RealmServer.Editor.Figurines
{
	public class WaypointFigurine : EditorFigurine
	{
		/// <summary>
		/// Scales the figurine in relation to its original version
		/// </summary>
		[NotVariable]
		public static float WPFigurineScale = 0.4f;

		private readonly NPCSpawnPoint m_SpawnPoint;
		private readonly WaypointEntry m_Waypoint;

		public WaypointFigurine(MapEditor editor, NPCSpawnPoint spawnPoint, WaypointEntry wp)
			: base(editor, spawnPoint.SpawnEntry.Entry)
		{	
			m_SpawnPoint = spawnPoint;
			m_Waypoint = wp;
			m_position = wp.Position;

			//GossipMenu = m_SpawnPoint.GossipMenu;
			NPCFlags = NPCFlags.Gossip;
		}

		public WaypointEntry Waypoint
		{
			get { return m_Waypoint; }
		}

		public override float DefaultScale
		{
			get
			{
				return WPFigurineScale;
			}
		}

		public override Faction Faction
		{
			get { return m_entry.Faction; }
			set { }
		}
	}
}