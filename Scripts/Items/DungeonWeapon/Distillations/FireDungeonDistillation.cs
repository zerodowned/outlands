﻿using System;
using Server;
using Server.Targeting;
using Server.Network;
using Server.Mobiles;

namespace Server.Items
{
    public class FireDungeonDistillation : DungeonDistillation
    {         
        [Constructable]
        public FireDungeonDistillation(): base()
        {
            Dungeon = DungeonEnum.Fire;            
        }

        public FireDungeonDistillation(Serial serial): base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0); //version       
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }
}