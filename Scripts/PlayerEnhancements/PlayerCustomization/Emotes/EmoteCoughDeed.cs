using System;
using Server;
using Server.Regions;
using Server.Targeting;
using Server.Network;
using Server.Mobiles;
using Server.Gumps;
using Server.Multis;
using System.Collections;
using System.Collections.Generic;
using Server.Items;

namespace Server.Custom
{
    public class EmoteCoughDeed : PlayerCustomizationDeed
    {
        [Constructable]
        public EmoteCoughDeed(): base()
        {
            Name = "Emote: Cough";

            Customization = CustomizationType.EmoteCough;
        }

        public override void OnSingleClick(Mobile from)
        {
            LabelTo(from, Name);
            LabelTo(from, "(player customization deed)");
        }

        public EmoteCoughDeed(Serial serial): base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }
}