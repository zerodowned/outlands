using System;
using Server;

namespace Server.Items
{
    public class HeaterShield : BaseShield
    {
        public override int BasePhysicalResistance { get { return 0; } }
        public override int BaseFireResistance { get { return 1; } }
        public override int BaseColdResistance { get { return 0; } }
        public override int BasePoisonResistance { get { return 0; } }
        public override int BaseEnergyResistance { get { return 0; } }

        public override int InitMinHits { get { return 100; } }
        public override int InitMaxHits { get { return 100; } }

        public override int AosStrReq { get { return 90; } }

        public override int ArmorBase { get { return 22; } }
        public override int OldDexBonus { get { return -6; } }

        public override int IconItemId { get { return 7031; } }
        public override int IconHue { get { return Hue; } }
        public override int IconOffsetX { get { return 0; } }
        public override int IconOffsetY { get { return 5; } }

        [Constructable]
        public HeaterShield(): base(7031)
        {
            Name = "heater shield";
            Weight = 8.0;
        }

        public HeaterShield(Serial serial): base(serial)
        {
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);//version
        }
    }
}
