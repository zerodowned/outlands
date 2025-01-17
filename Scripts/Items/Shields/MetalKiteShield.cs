using System;
using Server;

namespace Server.Items
{
    public class MetalKiteShield : BaseShield, IDyable
    {
        public override int BasePhysicalResistance { get { return 0; } }
        public override int BaseFireResistance { get { return 0; } }
        public override int BaseColdResistance { get { return 0; } }
        public override int BasePoisonResistance { get { return 0; } }
        public override int BaseEnergyResistance { get { return 1; } }

        public override int InitMinHits { get { return 95; } }
        public override int InitMaxHits { get { return 95; } }

        public override int AosStrReq { get { return 45; } }

        public override int ArmorBase { get { return 20; } }
        public override int OldDexBonus { get { return -5; } }

        public override int IconItemId { get { return 7029; } }
        public override int IconHue { get { return Hue; } }
        public override int IconOffsetX { get { return 4; } }
        public override int IconOffsetY { get { return 1; } }

        [Constructable]
        public MetalKiteShield(): base(7029)
        {
            Name = "metal kite shield";
            Weight = 7.0;
        }

        public MetalKiteShield(Serial serial): base(serial)
        {
        }

        public bool Dye(Mobile from, DyeTub sender)
        {
            if (Deleted)
                return false;

            Hue = sender.DyedHue;

            return true;
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            if (Weight == 5.0)
                Weight = 7.0;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);//version
        }
    }
}
