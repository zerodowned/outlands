using System;
using Server;

namespace Server.Custom.Townsystem
{
	public class JhelomFac : Faction
	{
		private static Faction m_Instance;

		public static Faction Instance{ get{ return m_Instance; } }

        public JhelomFac()
		{
			m_Instance = this;

			Definition =
				new FactionDefinition(
					3,
                    false,
					1761, // blue
                    1761, // FLAG HUE: Blue
                    1761, // join stone : blue
                    1761, // broadcast : blue
                    5653, 5654, 
					"Jhelom", "Jhelom", "Jhelom",
					"Jhelom",
					"Jhelom faction",
					"<center>Jhelom</center>",
					    "The council of Mages have their roots in the city of Moonglow, where " +
						"they once convened. They began as a small movement, dedicated to " +
						"calling forth the Stranger, who saved the lands once before.  A " +
						"series of war and murders and misbegotten trials by those loyal to " +
						"Lord British has caused the group to take up the banner of war.",
                    8741, //CRYSTAL ID
					"This city is controlled by Jhelom.",
					"This sigil has been corrupted by Jhelom",
					"The faction signup stone for Jhelom",
					"The Faction Stone of Jhelom",
					": Jhelom",
					"Members of Jhelom will now be ignored.",
					"Members of Jhelom will now be warned to leave.",
					"Members of Jhelom will now be beaten with a stick.",
					
					new RankDefinition[]
					{
						new RankDefinition( 10, 991, "Hero of Jhelom"),
                        new RankDefinition(  9, 950, "Champion of Jhelom"),
                        new RankDefinition(  8, 900, "Ascended Knight of Jhelom"),
                        new RankDefinition(  7, 800, "Gladiator of Jhelom"),
                        new RankDefinition(  6, 700, "Arena Master of Jhelom"),
                        new RankDefinition(  5, 600, "Knight of Jhelom"),
                        new RankDefinition(  4, 500, "Pit Fighter of Jhelom"),
                        new RankDefinition(  3, 400, "Jhelom Fighter"),
                        new RankDefinition(  2, 200, "Peasant-Warrior  of Jhelom"),
                        new RankDefinition(  1,   0, "Peasant-Warrior of Jhelom")
					},
					new GuardDefinition[]
					{
						new GuardDefinition( typeof( FactionHenchman ),		0x1403, 1500, 150, 10,		"HENCHMAN", "Hire Henchman"),
						new GuardDefinition( typeof( FactionMercenary ),	0x0F62, 2000, 200, 10,		new TextDefinition( 1011527, "MERCENARY" ),		new TextDefinition( 1011511, "Hire Mercenary" ) ),
						new GuardDefinition( typeof( FactionSorceress ),	0x0E89, 2500, 250, 10,		new TextDefinition( 1011507, "SORCERESS" ),		new TextDefinition( 1011501, "Hire Sorceress" ) ),
					    new GuardDefinition( typeof( FactionWizard ),		0x13F8, 3000, 300, 10,		new TextDefinition( 1011508, "ELDER WIZARD" ),	new TextDefinition( 1011502, "Hire Elder Wizard" ) ),
					}
				);
		}
	}
}