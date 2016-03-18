using System;
using Server.Items;
using Server.Mobiles;
using Server;
using Server.Engines.Quests;

namespace Server.Items
{
    public class DreadPlateHelm : BaseArmor
    {
        public override int PlayerClassCurrencyValue { get { return 125; } }

        public override int BasePhysicalResistance { get { return 5; } }
        public override int BaseFireResistance { get { return 3; } }
        public override int BaseColdResistance { get { return 2; } }
        public override int BasePoisonResistance { get { return 3; } }
        public override int BaseEnergyResistance { get { return 2; } }

        public override int InitMinHits { get { return 100; } }
        public override int InitMaxHits { get { return 120; } }

        public override int AosStrReq { get { return 80; } }
        public override int OldStrReq { get { return 40; } }

        public override int OldDexBonus { get { return -1; } }
       
        public override int ArmorBase { get { return 50; } }        
        public override int RevertArmorBase { get { return 5; } }

        public override ArmorMaterialType MaterialType { get { return ArmorMaterialType.Plate; } }

        [Constructable]
        public DreadPlateHelm(): base(0x1412)
        {
            Weight = 5.0;
            Name = "Dread Plate Helm";   

            PlayerClass = PlayerClass.Murderer;
            PlayerClassRestricted = true;
            Hue = PlayerClassPersistance.MurdererItemHue;
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            PlayerClassPersistance.PlayerClassSingleClick(this, from);
        }

        public DreadPlateHelm(Serial serial): base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            if (Weight == 1.0)
                Weight = 5.0;
        }
    }
}