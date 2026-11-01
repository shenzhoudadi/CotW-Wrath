using System.Collections.Generic;
using Kingmaker.Enums.Damage;
namespace CallOfTheWild.Wrath.Prototype {
 internal static class ChosenDeityAlignments {
  // Native deity identities, independent of providers that strip granted alignment facts.
  internal static readonly Dictionary<string,DamageAlignment> Native=new Dictionary<string,DamageAlignment> {
   {"6122dacf418611540a3c91e67197ee4e",DamageAlignment.Lawful}, // AbadarFeature
   {"a3189d5b7c4d4d91beaa8bfffac3e38e",DamageAlignment.Evil|DamageAlignment.Lawful}, // AchaekekFeature
   {"772e2673945e4583a804ae01f67efea0",DamageAlignment.Good|DamageAlignment.Lawful}, // ApsuFeature
   {"d714ecb5d5bb89a42957de0304e459c9",DamageAlignment.Evil|DamageAlignment.Chaotic}, // AreshkagalFeature
   {"a3a5ccc9c670e6f4ca4a686d23b89900",DamageAlignment.Evil|DamageAlignment.Lawful}, // AsmodeusFeature
   {"92c0d2da0a836ce418a267093c09ca54",(DamageAlignment)0}, // AtheismFeature
   {"bd72ca8ffcfec5745899ac56c93f12c5",DamageAlignment.Evil|DamageAlignment.Chaotic}, // BaphometFeature
   {"c7531715a3f046d4da129619be63f44c",DamageAlignment.Chaotic}, // CalistriaFeature
   {"300e212868bca984687c92bcb66d381b",DamageAlignment.Good|DamageAlignment.Chaotic}, // CaydenCaileanFeature
   {"8f7118d68f6e44dea94dddb51f38cbdd",DamageAlignment.Evil|DamageAlignment.Chaotic}, // DahakFeature
   {"ddf913858bdf43b4da3b731e082fbcc0",DamageAlignment.Evil|DamageAlignment.Chaotic}, // DeskariFeature
   {"2c0a3b9971327ba4d9d85354d16998c1",DamageAlignment.Good|DamageAlignment.Chaotic}, // DesnaFeature
   {"470d824767d34e2e9a59b6d70681af7a",DamageAlignment.Chaotic}, // DLC3_BesmaraFeature
   {"afc775188deb7a44aa4cbde03512c671",DamageAlignment.Good|DamageAlignment.Lawful}, // ErastilFeature
   {"c4403567ea9e5fe478ca38e3052fadc2",(DamageAlignment)0}, // ForbiddenDeityFeature
   {"583a26e88031d0a4a94c8180105692a5",DamageAlignment.Lawful}, // GodclawFeature
   {"8f49a5d8528a82c44b8c117a89f6b68c",DamageAlignment.Chaotic}, // GorumFeature
   {"4af983eec2d821b40a3065eb5e8c3a72",(DamageAlignment)0}, // GozrehFeature
   {"ca763809e01f4247a3639965364c26cb",(DamageAlignment)0}, // GreenFaithCameliaFeature
   {"99a7a8f13c1300c42878558fa9471e2f",(DamageAlignment)0}, // GreenFaithFeature
   {"c3e4d5681906d5246ab8b0637b98cbfe",DamageAlignment.Chaotic}, // GroetusFeature
   {"8b535b6842e063d48a571a042c3c6e8f",DamageAlignment.Evil|DamageAlignment.Chaotic}, // GyronnaFeature
   {"88d5da04361b16746bf5b65795e0c38c",DamageAlignment.Good|DamageAlignment.Lawful}, // IomedaeFeature
   {"23a77a5985de08349820429ce1b5a234",DamageAlignment.Lawful}, // IroriFeature
   {"f12c1ccc9d600c04f8887cd28a8f45a5",DamageAlignment.Evil|DamageAlignment.Chaotic}, // KabririFeature
   {"f86bc8fbf13221f4f9041608a1fb8585",DamageAlignment.Evil|DamageAlignment.Chaotic}, // LamashtuFeature
   {"b4153b422d02d4f48b3f8f0ceb6a10eb",DamageAlignment.Evil|DamageAlignment.Chaotic}, // LichDeityFeature
   {"6262cfce7c31626458325ca0909de997",(DamageAlignment)0}, // NethysFeature
   {"805b6bdc8c96f4749afc687a003f9628",DamageAlignment.Evil}, // NorgorberFeature
   {"458750bc214ab2e44abdeae404ab22e9",(DamageAlignment)0}, // PharasmaFeature
   {"ebb0b46f95dbac74681c78aae895dbd0",DamageAlignment.Good|DamageAlignment.Chaotic}, // PuluraFeature
   {"04bc2b62273ab744092d992ed72bff41",DamageAlignment.Evil|DamageAlignment.Chaotic}, // RovagugFeature
   {"c1c4f7f64842e7e48849e5e67be11a1b",DamageAlignment.Good}, // SarenraeFeature
   {"b382afa31e4287644b77a8b30ed4aa0b",DamageAlignment.Good}, // ShelynFeature
   {"d2d5c5a58885a6b489727467e13c3337",DamageAlignment.Good|DamageAlignment.Lawful}, // ToragFeature
   {"812f6c07148088e41a9ac94b56ac2fc8",DamageAlignment.Evil}, // UrgathoaFeature
   {"f7eed400baa66a744ad361d4df0e6f1b",DamageAlignment.Evil|DamageAlignment.Lawful}, // ZonKuthonFeature
  };
 }
}
